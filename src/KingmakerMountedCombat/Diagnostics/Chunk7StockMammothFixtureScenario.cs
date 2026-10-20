using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Disclosed reference-free fixture production. The only progression write is
    // one bounded native GainExperience setup; seven levels and the stock pet
    // choice are committed by Kingmaker's ordinary builder. No Mount occurs here.
    internal sealed class Chunk7StockMammothFixtureScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-stock-mammoth-fixture-evidence.json";
        internal const string EvidenceKind = "chunk7-stock-mammoth-fixture-evidence";
        internal const string PendingFileName = "chunk7-stock-mammoth-fixture-cleanup-pending.json";
        internal const string PendingKind = "chunk7-stock-mammoth-fixture-cleanup-pending";
        internal static bool SupportsScenario(string scenario) => scenario == Chunk7StockMammothFixtureSelection.Scenario;

        private readonly RuntimeRequest request;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly WorkingFixtureLoader loader;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly MountedPersistenceService persistence;
        private readonly Game game;
        private readonly SaveManager manager;
        private readonly Player menuPlayer;
        private readonly int generation;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly JArray choices = new JArray();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private Chunk7BootstrapInventoryProbe loadProbe;
        private NativeArchiveRestorationProbe restoration;
        private Chunk7SmallFixtureSaveProbe saveProbe;
        private Chunk7NativeWorldScope scope;
        private Chunk7OrdinaryBuildDriver driver;
        private UnitEntityData rider, mammoth;
        private UnitEntityData[] originalCharacters, originalSelection;
        private SaveInfo sourceDescriptor, requestedManual;
        private string sourcePath;
        private long sourceLength, sourceTicks;
        private MovementCaptureOverlayLease ummLease;
        private JObject beforeSave;
        private JObject neighboursBefore;
        private long initialMoney;
        private bool originalPause, presentationCaptured, begun, admitted, assisted, saveRequested, saveCallback, saveProven;
        private bool cleanup, completed, settled, disposed, pending;
        private double cleanupStart;
        private int stage, callbackCount;

        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;

        internal Chunk7StockMammothFixtureScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, WorkingFixtureLoader loader, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence)
        {
            if (request?.StockMammothFixture == null || request.Validate().Count != 0 ||
                !Chunk7StockMammothFixtureSelection.IsSource(request.PersistenceLoad))
                throw new ArgumentException("Exact stock Mammoth clean-seed fixture request is required.");
            this.request = request; this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            game = Game.Instance ?? throw new InvalidOperationException("Stock Mammoth pre-load Game is unavailable.");
            manager = game.SaveManager ?? throw new InvalidOperationException("Stock Mammoth pre-load SaveManager is unavailable.");
            menuPlayer = game.Player; generation = authorization.AuthorityGeneration;
        }

        internal void BeginBeforeLoad()
        {
            Require(!begun && !disposed, "Stock Mammoth observers may be installed once before load.");
            begun = true; RequireAuthority(false); RequireNoOwners();
            Require(authority.AdmittedNativeLoadRequests == 0 && authorization.AuthorizedLoadCount == 0 &&
                loader.State == WorkingFixtureLoadState.NotStarted && !loader.LoadCallbackObserved && loader.Descriptor == null &&
                game.CurrentlyLoadedArea == null && ReferenceEquals(game.Player, menuPlayer) && menuPlayer?.MainCharacter.Value == null &&
                Game.NewGameUnit == null && !Busy() && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Stock Mammoth producer requires an idle empty menu before its sole native seed load.");
            loadProbe = new Chunk7BootstrapInventoryProbe(game); loadProbe.Install();
            restoration = new NativeArchiveRestorationProbe(persistence); restoration.Install();
            sourcePath = Path.Combine(authority.Root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath);
            Require(source.Exists && source.Length == request.PersistenceLoad.Length &&
                (source.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(sourcePath) == request.PersistenceLoad.Sha256,
                "Stock Mammoth immutable clean-seed input is absent or changed before native load.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks;
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(request.StockMammothFixture);
            evidence["source"] = Chunk7EvidenceDto.Request(request.PersistenceLoad);
            evidence["contract"] = "owner-seed-reference-free-stock-mammoth-producer-v1";
            evidence["fixtureSetupOnly"] = true; evidence["ordinaryAcquisitionQualified"] = false;
            evidence["positiveRemovalQualified"] = false; evidence["dllAbsentLoadQualified"] = false;
            evidence["choices"] = choices; evidence["beforeLoadAuthority"] = AuthoritySnapshot();
            Pass("C7-stock-mammoth-source");
        }

        internal void Update()
        {
            if (completed || disposed) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                Require(begun && clock.Elapsed.TotalSeconds <= 900, "Stock Mammoth fixture exceeded its bounded native setup window.");
                RequireAuthority(false);
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown,
                    "Unexpected dialog blocks stock Mammoth native setup.");
                if (Busy()) return;
                if (admitted) { RequireWorld(); RequireRoster(); RequireNeighbours(); RequireNoOwners(); }
                switch (stage)
                {
                    case 0: BindColdWorld(); break;
                    case 1:
                        if (driver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                        mammoth = rider.Descriptor.Pet;
                        RequireStockMammoth(); scope.Track(mammoth);
                        Require(driver.NativeCommitCount == 7 && driver.AutoLevelupOverrideCount == 0,
                            "Stock Mammoth Ranger three-to-ten build did not commit exactly seven native levels.");
                        evidence["nativeBuild"] = driver.CaptureState();
                        evidence["nativeCommitCount"] = driver.NativeCommitCount;
                        evidence["stockMammoth"] = StockMammothObservation();
                        Pass("C7-stock-mammoth-native-choices"); stage = 2; break;
                    case 2:
                        if (!Ready(rider) || !Ready(mammoth)) return;
                        RequireStockMammoth(); beforeSave = Arrangement(); BeginManual(); break;
                    case 3: PollManual(); break;
                    default: throw new InvalidOperationException("Unknown stock Mammoth fixture phase.");
                }
            }
            catch (Exception error)
            {
                Fail("C7-stock-mammoth-runtime", "stage=" + stage + ";commits=" +
                    (driver?.NativeCommitCount ?? 0) + "; " + error);
                BeginCleanup();
            }
        }

        private void BindColdWorld()
        {
            if (loader.State != WorkingFixtureLoadState.LoadedAndVerified || !loader.LoadCallbackObserved || loadProbe?.Complete != true) return;
            Require(authority.AdmittedNativeLoadRequests == 1 && authorization.AuthorizedLoadCount == 1 &&
                ReferenceEquals(loadProbe.ObservedPlayer, game.Player) && loader.Descriptor != null &&
                loader.Descriptor.FileName == request.PersistenceLoad.FileName &&
                authority.ValidateArchiveIdentity(Target(loader.Descriptor), authority.Root) == null,
                "Stock Mammoth source did not complete its exact native load and verified descriptor.");
            if (game.CurrentMode != GameModeType.Default || game.CutsceneLock || game.IsModeActive(GameModeType.Dialog)) return;
            Require(game.Player != null && game.State?.LoadedAreaState?.MainState != null && !game.Player.IsInCombat &&
                game.Player.GameId == Chunk7SmallFixtureSelection.CampaignId &&
                game.Player.MainCharacter.Value?.UniqueId == Chunk7SmallFixtureSelection.MainActorId &&
                game.Player.MainCharacter.Value.CharacterName == Chunk7SmallFixtureSelection.CampaignName &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == Chunk7SmallFixtureSelection.AreaId &&
                persistence.LoadedArchive == null && persistence.LoadedArchiveKind == MountedSaveReadKind.Missing &&
                !persistence.LoadedArchiveCanRestore,
                "Stock Mammoth producer did not load the exact clean owner campaign on the missing-metadata path.");
            rider = game.Player.MainCharacter.Value;
            originalCharacters = game.Player.PartyCharacters.Take(7).Select(value => value.Value).ToArray();
            var ids = new[] { Chunk7SmallFixtureSelection.MainActorId, "5dd7bd1d-ae4c-4df7-9687-50fceb027344",
                "8265f8cf-0969-4ec7-a266-2448de91c8c4", "fd213210-71cd-41b0-b35b-68e1b18e9b43" };
            Require(originalCharacters.Length == 4 && originalCharacters.All(value => Ready(value) && value.Descriptor.Pet == null) &&
                originalCharacters.Select(value => value.UniqueId).OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(ids.OrderBy(value => value, StringComparer.Ordinal)) &&
                !game.Player.RemoteCompanions.Any() && Chunk7NativeFieldRoster.Capture(game.Player).Length == 4,
                "Stock Mammoth clean seed must retain its four exact petless field characters.");
            scope = new Chunk7NativeWorldScope(request, originalCharacters); admitted = true;
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothFeatureGuid);
            Require(ranger != null && feature != null && game.Player.Money >= 0 &&
                rider.Descriptor.Progression.CharacterLevel == 3 && rider.Descriptor.Progression.GetClassLevel(ranger) == 3 &&
                rider.Descriptor.Progression.Experience == Chunk7StockMammothFixtureSelection.StartingExperience &&
                LevelUpController.CanLevelUp(rider.Descriptor) && rider.Descriptor.OriginalSize == Size.Medium &&
                rider.Descriptor.State.Size == Size.Medium && rider.GetActivePolymorph() == null &&
                BlueprintRoot.Instance.Progression.XPTable.GetBonus(10) == Chunk7StockMammothFixtureSelection.TargetExperience,
                "Stock Mammoth seed cannot follow the selected exact Ranger-three-to-ten native plan.");
            Require(restoration.Complete && restoration.Capture()["events"] is JArray events && events.Count == 0,
                "Reference-free missing-metadata source observed unexpected native preparation or KMC restoration.");
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray(); originalPause = game.IsPaused;
            neighboursBefore = CaptureNeighbours(); initialMoney = game.Player.Money;
            evidence["neighboursBefore"] = neighboursBefore.DeepClone(); evidence["initialMoney"] = initialMoney;
            Require(originalSelection.Distinct().Count() == originalSelection.Length &&
                originalSelection.All(value => originalCharacters.Contains(value)), "Stock Mammoth seed selection is foreign or duplicate.");
            presentationCaptured = true; MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            game.IsPaused = false;
            evidence["loadedWorld"] = WorldSnapshot(); evidence["noPreparation"] = restoration.Capture();
            ApplyDeclaredExperience();
            driver = new Chunk7OrdinaryBuildDriver(new Chunk7OrdinaryBuildPins(request.RunId,
                request.StockMammothFixture.SourceInventorySha256, Chunk7OrdinaryBuildMode.ExistingRangerLevels,
                null, null, null, Gender.Male, false, 10, Size.Medium, false, true), rider, feature,
                HorseCompanionBlueprintService.MammothUnitGuid, RequireChoiceAuthority, ObserveChoice);
            stage = 1; Pass("C7-stock-mammoth-native-load");
        }

        private void ApplyDeclaredExperience()
        {
            RequireChoiceAuthority(); Require(!assisted, "Stock Mammoth XP assistance may occur once.");
            var progression = rider.Descriptor.Progression;
            Chunk7AssistedFixtureNativeBoundary.RequireMethod(typeof(UnitProgressionData), "GainExperience", 0x06001F80,
                typeof(void), new[] { typeof(int), typeof(bool) });
            var before = progression.Experience; var level = progression.CharacterLevel; var money = game.Player.Money;
            var delta = request.StockMammothFixture.NativeXpAssistance;
            Require(before == Chunk7StockMammothFixtureSelection.StartingExperience &&
                delta == Chunk7StockMammothFixtureSelection.DisclosedNativeXpAssistance &&
                checked(before + delta) == Chunk7StockMammothFixtureSelection.TargetExperience,
                "Stock Mammoth one-time native XP setup differs from its disclosed bounds.");
            progression.GainExperience(delta, false); assisted = true;
            Require(progression.Experience == Chunk7StockMammothFixtureSelection.TargetExperience &&
                progression.CharacterLevel == level && game.Player.Money == money &&
                LevelUpController.CanLevelUp(rider.Descriptor),
                "Native XP assistance changed more than the rider's declared experience.");
            RequireNeighbours();
            evidence["xpAssistance"] = new JObject { ["method"] = "UnitProgressionData.GainExperience(int, false)",
                ["once"] = true, ["before"] = before, ["after"] = progression.Experience, ["delta"] = delta,
                ["levelBefore"] = level, ["levelAfter"] = progression.CharacterLevel,
                ["moneyBefore"] = money, ["moneyAfter"] = game.Player.Money,
                ["ordinaryAcquisitionProof"] = false };
        }

        private void RequireStockMammoth()
        {
            var rank = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.LevelRankGuid);
            var upgrade = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothUpgradeGuid);
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothFeatureGuid);
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            Require(rider != null && mammoth != null && rank != null && upgrade != null && feature != null && ranger != null &&
                rider.Descriptor.Progression.CharacterLevel == 10 && rider.Descriptor.Progression.GetClassLevel(ranger) == 10 &&
                rider.Descriptor.Progression.Experience == Chunk7StockMammothFixtureSelection.TargetExperience &&
                rider.Descriptor.GetFact(rank)?.GetRank() >= 7 && rider.Descriptor.GetFact(feature) is Feature fact && fact.Source != null &&
                mammoth.Blueprint?.AssetGuid == HorseCompanionBlueprintService.MammothUnitGuid &&
                ReferenceEquals(rider.Descriptor.Pet, mammoth) && ReferenceEquals(mammoth.Descriptor.Master.Value, rider) &&
                mammoth.Descriptor.GetFact(upgrade) != null && rider.Descriptor.State.Size == Size.Medium &&
                mammoth.Descriptor.State.Size == Size.Large && rider.GetActivePolymorph() == null &&
                mammoth.GetActivePolymorph() == null,
                "Native Ranger-ten stock Mammoth lacks reciprocal rank-seven Large field eligibility.");
        }

        private JObject StockMammothObservation()
        {
            RequireStockMammoth();
            var rank = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.LevelRankGuid);
            var feature = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothFeatureGuid);
            var upgrade = ResourcesLibrary.TryGetBlueprint<BlueprintFeature>(HorseCompanionBlueprintService.MammothUpgradeGuid);
            return new JObject { ["riderId"] = rider.UniqueId, ["riderObject"] = loadProbe.Identity(rider),
                ["petId"] = mammoth.UniqueId, ["petObject"] = loadProbe.Identity(mammoth),
                ["petBlueprint"] = mammoth.Blueprint.AssetGuid, ["featureGuid"] = feature.AssetGuid,
                ["featureSource"] = ((Feature)rider.Descriptor.GetFact(feature)).Source.AssetGuid,
                ["rankGuid"] = rank.AssetGuid, ["rank"] = rider.Descriptor.GetFact(rank).GetRank(),
                ["upgradeGuid"] = upgrade.AssetGuid, ["upgradePresent"] = mammoth.Descriptor.GetFact(upgrade) != null,
                ["rangerLevel"] = rider.Descriptor.Progression.CharacterLevel,
                ["riderSize"] = rider.Descriptor.State.Size.ToString(),
                ["petSize"] = mammoth.Descriptor.State.Size.ToString(),
                ["petMasterId"] = mammoth.Descriptor.Master.Value.UniqueId };
        }

        private void BeginManual()
        {
            RequireChoiceAuthority(); RequireStockMammoth(); Require(!saveRequested && !Busy() &&
                game.UI.CharacterBuildController.LevelUpController == null && game.UI.CharacterBuildController.Unit == null &&
                ReferenceEquals(game.Player.SavedInArea, game.CurrentlyLoadedArea) && manager.IsSaveAllowed() &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled && !loadProbe.InSynchronousCallback &&
                authority.AdmittedNativeWriteRequests == 0 && authority.StockMammothFixtureReservations == 0,
                "Stock Mammoth native builder must settle before one genuine Manual.");
            var saves = manager.Take(3).ToArray();
            sourceDescriptor = saves.Length == 1 ? saves[0] : null;
            Require(sourceDescriptor != null && loader.Descriptor != null && sourceDescriptor.Type == SaveInfo.SaveType.Manual &&
                sourceDescriptor.IsActuallySaved && sourceDescriptor.HasFileOnDisk && sourceDescriptor.OperationState == SaveInfo.StateType.None &&
                sourceDescriptor.FileName == request.PersistenceLoad.FileName && sourceDescriptor.FolderName == loader.Descriptor.FolderName &&
                sourceDescriptor.Name == loader.Descriptor.Name && sourceDescriptor.GameId == loader.Descriptor.GameId &&
                authority.ValidateArchiveIdentity(Target(sourceDescriptor), authority.Root) == null,
                "Stock Mammoth source descriptor differs from its sole immutable seed.");
            var number = manager.FindUnusedSaveNumber(SaveInfo.SaveType.Manual);
            var leaf = Chunk7StockMammothFixtureSelection.ManualLeaf(number);
            Require(!File.Exists(Path.Combine(authority.Root, leaf)), "Stock Mammoth native Manual output already exists.");
            authority.ReserveStockMammothFixtureOutput(request, game, manager, number);
            Require(authority.StockMammothFixtureOutputLeaf == leaf && authority.StockMammothFixtureReservations == 1,
                "Stock Mammoth Manual reservation lost its exact native slot.");
            evidence["nativeNumbering"] = new JObject { ["method"] = "FindUnusedSaveNumber", ["number"] = number,
                ["fileName"] = leaf, ["outputInitiallyAbsent"] = true, ["slotProjectionEnabled"] = false };
            saveProbe = new Chunk7SmallFixtureSaveProbe(game, persistence, loadProbe.Identity, RequireWorld,
                leaf, Chunk7StockMammothFixtureSelection.OutputName); saveProbe.Install();
            requestedManual = manager.CreateNewSave(Chunk7StockMammothFixtureSelection.OutputName);
            Require(requestedManual != null && requestedManual.Type == SaveInfo.SaveType.Manual &&
                requestedManual.Name == Chunk7StockMammothFixtureSelection.OutputName &&
                !requestedManual.IsActuallySaved && !requestedManual.HasFileOnDisk &&
                ReferenceEquals(requestedManual.Area, game.CurrentlyLoadedArea),
                "Stock Mammoth native new-save descriptor differs from its separate Manual.");
            saveProbe.Expect(requestedManual); saveRequested = true; stage = 3;
            game.SaveGame(requestedManual, () => { callbackCount++; saveCallback = true; saveProbe.ObserveCallback(); });
        }

        private void PollManual()
        {
            RequireAuthority(false); RequireWorld(); RequireRoster(); RequireNoOwners();
            if (!saveCallback) return;
            Require(callbackCount == 1 && saveProbe?.Complete == true && !Busy() &&
                authority.AdmittedNativeLoadRequests == 1 && authority.AdmittedNativeWriteRequests == 1 &&
                authorization.AuthorizedLoadCount == 1 && authorization.AuthorizedWriteCount == 1 &&
                persistence.SnapshotCount == 1 && persistence.CompletedSaveCount == 1,
                "Stock Mammoth genuine Manual did not settle its worker, snapshot and callback exactly once.");
            var completion = persistence.LastCompletedSave; var written = saveProbe.Written;
            Require(completion != null && completion.Sequence == 1 && ReferenceEquals(completion.Requested, requestedManual) &&
                ReferenceEquals(completion.Written, written) && completion.Path == Path.Combine(authority.Root, authority.StockMammothFixtureOutputLeaf) &&
                written.Type == SaveInfo.SaveType.Manual && written.Name == Chunk7StockMammothFixtureSelection.OutputName &&
                written.FileName == authority.StockMammothFixtureOutputLeaf && written.GameId == request.PersistenceLoad.GameId &&
                written.GameName == request.PersistenceLoad.GameName && written.Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area &&
                written.IsActuallySaved && written.HasFileOnDisk && written.OperationState == SaveInfo.StateType.None,
                "Stock Mammoth Manual completion differs from its exact requested/prepared native descriptors.");
            var read = NativeMountedSaveStorage.ReadArchive(written.Saver); var archive = read.Data;
            Require(read.Kind == MountedSaveReadKind.Current && read.SourceSchema == 3 && archive != null,
                "Stock Mammoth native output lacks current schema-3 metadata readback.");
            Chunk7SmallFixtureScenario.RequireEmptyArchive(archive, request.PersistenceLoad.GameId, request.PersistenceLoad.Area);
            RequireStockMammoth(); var after = Arrangement();
            Require(JToken.DeepEquals(beforeSave, after) && restoration.Complete &&
                restoration.Capture()["events"] is JArray events && events.Count == 0 &&
                !controls.SerializationSuspended && !persistence.SaveSuspended,
                "Stock Mammoth native save changed progression/ownership or left restoration custody.");
            RequireNeighbours(); evidence["neighboursAfter"] = CaptureNeighbours();
            Require(JToken.DeepEquals(evidence["stockMammoth"], StockMammothObservation()),
                "Stock Mammoth rank, feature, size or reciprocal owner changed during native save.");
            var file = new FileInfo(completion.Path);
            Require(file.Exists && file.Length > 0 && (file.Attributes & FileAttributes.ReparsePoint) == 0 &&
                file.FullName == written.FolderName, "Stock Mammoth Manual did not materialize as its exact regular file.");
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["fileName"] = written.FileName,
                ["internalName"] = written.Name, ["type"] = written.Type.ToString(), ["sha256"] = Hash(file.FullName),
                ["length"] = file.Length, ["lastWriteTimeUtcTicks"] = file.LastWriteTimeUtc.Ticks,
                ["gameId"] = written.GameId, ["gameName"] = written.GameName,
                ["area"] = written.Area.AssetGuidThreadSafe, ["completionSequence"] = completion.Sequence,
                ["callbackCount"] = callbackCount, ["nativeWorkerSettled"] = true,
                ["readKind"] = read.Kind.ToString(), ["sourceSchema"] = read.SourceSchema,
                ["archive"] = JObject.FromObject(archive, MountedSaveCodec.CreateSerializer()) };
            evidence["beforeSaveArrangement"] = beforeSave; evidence["afterSaveArrangement"] = after;
            evidence["savedWorld"] = WorldSnapshot(); saveProven = true;
            Pass("C7-stock-mammoth-native-manual"); BeginCleanup();
        }

        private JObject Arrangement() => new JObject { ["gameId"] = game.Player.GameId, ["gameName"] = game.Player.MainCharacter.Value.CharacterName,
            ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe, ["money"] = game.Player.Money,
            ["party"] = new JArray(Chunk7NativeFieldRoster.Capture(game.Player).OrderBy(value => value.UniqueId, StringComparer.Ordinal)
                .Select(value => new JObject { ["id"] = value.UniqueId, ["object"] = loadProbe.Identity(value),
                    ["descriptor"] = loadProbe.Identity(value.Descriptor), ["view"] = loadProbe.Identity(value.View),
                    ["blueprint"] = value.Blueprint.AssetGuid,
                    ["level"] = value.Descriptor.Progression.CharacterLevel, ["xp"] = value.Descriptor.Progression.Experience,
                    ["size"] = value.Descriptor.State.Size.ToString(), ["pet"] = value.Descriptor.Pet?.UniqueId,
                    ["master"] = value.Descriptor.Master.Value?.UniqueId })) };
        private JObject WorldSnapshot() => new JObject { ["arrangement"] = Arrangement(),
            ["game"] = loadProbe.Identity(game), ["saveManager"] = loadProbe.Identity(manager),
            ["player"] = loadProbe.Identity(game.Player), ["state"] = loadProbe.Identity(game.State),
            ["areaState"] = loadProbe.Identity(game.State.LoadedAreaState),
            ["mainState"] = loadProbe.Identity(game.State.LoadedAreaState.MainState),
            ["mode"] = game.CurrentMode.ToString(), ["inCombat"] = game.Player.IsInCombat,
            ["owners"] = world.Registry.Owners.Length, ["slots"] = controls.CapturePersistentSlots().Length,
            ["casts"] = controls.NativeCastRequestCount,
            ["snapshots"] = persistence.SnapshotCount, ["completedSaves"] = persistence.CompletedSaveCount,
            ["authority"] = AuthoritySnapshot() };
        private void RequireChoiceAuthority()
        {
            Require(!cleanup && (stage == 0 || stage == 1 || stage == 2), "Stock Mammoth builder is outside its closed setup phase.");
            RequireAuthority(false); RequireWorld(); RequireRoster(); RequireNoOwners();
            Require(!Busy() && !game.Player.IsInCombat && !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog),
                "Stock Mammoth builder requires an idle world without a dialog or native worker.");
        }
        private void ObserveChoice(JObject row)
        { Require(choices.Count < 1400, "Stock Mammoth native choices exceeded their bounded evidence capacity."); choices.Add(row.DeepClone()); }
        private void RequireAuthority(bool cleanupOnly)
        {
            var root = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)),
                "runtime-staging", "persistence-" + request.RunId, "Saved Games");
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager) &&
                authorization.IsActive && authorization.AuthorityGeneration == generation &&
                authorization.IsExactPersistenceScope(authority) && NativePersistenceIsolation.IsExactAuthority(authority) &&
                authority.Root == root && manager.SavePath == root &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(root), "Areas") &&
                authority.MatchesStockMammothFixturePlan(request, game, manager) &&
                authority.AdmittedNativeLoadRequests <= 1 && authority.AdmittedNativeWriteRequests <= 1 &&
                authorization.AuthorizedLoadCount == authority.AdmittedNativeLoadRequests &&
                authorization.AuthorizedWriteCount == authority.AdmittedNativeWriteRequests &&
                !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Stock Mammoth producer lost its exact isolated one-load one-Manual authority.");
            if (!cleanupOnly) Require(authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 &&
                authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 &&
                authorization.BaselineLoadRequestCount == 0, "Stock Mammoth producer encountered an undeclared native boundary.");
        }
        private void RequireWorld() { Require(admitted && scope != null, "Stock Mammoth world is not admitted."); scope.RequireCurrent(); }
        private void RequireRoster()
        {
            RequireWorld(); var characters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            Require(characters.Length == 4 && originalCharacters.All(value => characters.Count(other => ReferenceEquals(value, other)) == 1) &&
                !game.Player.RemoteCompanions.Any() && originalCharacters.Where(value => !ReferenceEquals(value, rider))
                    .All(value => value.Descriptor.Pet == null) &&
                (mammoth == null || ReferenceEquals(rider.Descriptor.Pet, mammoth)) &&
                Chunk7NativeFieldRoster.Capture(game.Player).All(value => originalCharacters.Contains(value) ||
                    ReferenceEquals(value, rider.Descriptor.Pet)),
                "Stock Mammoth setup changed an unrelated character, remote companion or field roster.");
        }
        private JObject CaptureNeighbours() => new JObject { ["actors"] = new JArray(originalCharacters
            .Where(value => !ReferenceEquals(value, rider)).OrderBy(value => value.UniqueId, StringComparer.Ordinal)
            .Select(value =>
            {
                var progression = value.Descriptor.Progression;
                var classes = progression.Classes.Take(21).ToArray();
                var facts = progression.Features.Enumerable.Take(129).ToArray();
                var equipment = value.Descriptor.Body.CurrentEquipmentSlots.Take(13).ToArray();
                Require(classes.Length <= 20 && classes.All(item => item?.CharacterClass != null) &&
                    facts.Length <= 128 && facts.All(item => item?.Blueprint != null) &&
                    equipment.Length == 12 && equipment.All(item => item != null),
                    "Stock Mammoth neighbour observation exceeds its bounded native shape.");
                return new JObject { ["id"] = value.UniqueId, ["object"] = loadProbe.Identity(value),
                    ["descriptor"] = loadProbe.Identity(value.Descriptor), ["view"] = loadProbe.Identity(value.View),
                    ["blueprint"] = value.Blueprint.AssetGuid, ["pet"] = value.Descriptor.Pet?.UniqueId,
                    ["master"] = value.Descriptor.Master.Value?.UniqueId, ["xp"] = progression.Experience,
                    ["level"] = progression.CharacterLevel,
                    ["classes"] = new JArray(classes.OrderBy(item => item.CharacterClass.AssetGuid, StringComparer.Ordinal)
                        .Select(item => new JObject { ["guid"] = item.CharacterClass.AssetGuid, ["level"] = item.Level })),
                    ["facts"] = new JArray(facts.OrderBy(item => item.Blueprint.AssetGuid, StringComparer.Ordinal)
                        .Select(item => new JObject { ["guid"] = item.Blueprint.AssetGuid,
                            ["source"] = item.Source?.AssetGuid, ["rank"] = item.GetRank() })),
                    ["equipment"] = new JArray(equipment.Select(item => (JToken)new JValue(item.MaybeItem?.Blueprint?.AssetGuid))) };
            })) };
        private void RequireNeighbours()
        {
            Require(game.Player.Money == initialMoney && JToken.DeepEquals(neighboursBefore, CaptureNeighbours()),
                "Stock Mammoth setup changed owner money or an unrelated actor's progression, facts or equipment.");
        }
        private void RequireNoOwners(bool requireNoCast = true) => Require(world.Registry.Owners.Length == 0 &&
            controls.CapturePersistentSlots().Length == 0 && (!requireNoCast || controls.NativeCastRequestCount == 0) &&
            !controls.HasUnsettledRelationshipTransition &&
            !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(pair =>
                pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.PairOwner == null &&
                pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasActiveCommand &&
                !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership),
            "Stock Mammoth fixture must remain wholly unmounted and free of paired residue.");
        private bool Busy() => LoadingProcess.Instance != null &&
            (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending ||
            persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private static bool Ready(UnitEntityData value) => value != null && value.IsInState && value.IsInGame &&
            value.View?.AgentASP != null && value.Descriptor.State.IsConscious && !value.Descriptor.State.IsDead &&
            !value.IsInCombat && value.Commands != null && value.Commands.Empty && value.Commands.Queue != null &&
            value.Commands.Queue.Count == 0;
        private JObject AuthoritySnapshot() => new JObject { ["root"] = authority.Root, ["generation"] = generation,
            ["maximumNativeLoads"] = authority.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests,
            ["admittedNativeLoads"] = authority.AdmittedNativeLoadRequests, ["admittedNativeWrites"] = authority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount,
            ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount,
            ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount,
            ["reservations"] = authority.StockMammothFixtureReservations,
            ["manualNumber"] = authority.StockMammothFixtureNumber,
            ["outputLeaf"] = authority.StockMammothFixtureOutputLeaf };
        private static RuntimeSaveTarget Target(SaveInfo save) => new RuntimeSaveTarget { InternalName = save.Name,
            FileName = save.FileName, FullPath = save.FolderName, SaveType = save.Type.ToString(), GameId = save.GameId,
            GameName = save.GameName, Area = save.Area?.AssetGuidThreadSafe };
        private static string Hash(string path)
        { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private void Pass(string row)
        { if (results.All(value => value.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return; errors.Add(error);
            var result = results.SingleOrDefault(value => value.Name == row);
            if (result == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { result.Status = "FAIL"; result.AssertionFailCount++; result.Errors = result.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }

        private void BeginCleanup() { if (cleanup) return; cleanup = true; cleanupStart = clock.Elapsed.TotalSeconds; }
        private void PollCleanup()
        {
            try
            {
                RequireAuthority(true);
                if (controls.NativeCastRequestCount != 0)
                    Fail("C7-stock-mammoth-cleanup", "Stock Mammoth producer observed an unexpected native KMC cast request during setup or cleanup.");
                if (Busy() || loadProbe?.InSynchronousCallback == true || saveProbe?.InCallback == true ||
                    persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused)
                { Retain("Stock Mammoth native load/save worker or observer callback retains custody."); return; }
                if (admitted)
                {
                    RequireWorld();
                    if (driver != null && driver.TryGetOwnedBuildSession(out var builder))
                    {
                        if (cancellation == null)
                            cancellation = new Chunk7NativeBuilderCancellation(builder,
                                () => { RequireAuthority(true); RequireWorld(); }, Busy);
                        if (!cancellation.TryDrain()) { Retain("Stock Mammoth native builder cancellation retains custody."); return; }
                    }
                    RequireNoOwners(false);
                    if (presentationCaptured)
                    {
                        SelectionManager.Instance.MultiSelect(originalSelection.Where(value => value.IsInState && value.View != null)
                            .Select(value => value.View), false);
                        Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection),
                            "Stock Mammoth initial selection did not restore.");
                        ummLease?.Dispose(); ummLease = null; game.IsPaused = originalPause;
                    }
                }
                if (errors.Count == 0) Require(saveProven, "Stock Mammoth fixture cannot pass without a genuine native Manual.");
                var sourceUnchanged = false;
                try { VerifySource(); sourceUnchanged = true; }
                catch (Exception error) { Fail("C7-stock-mammoth-cleanup", error.ToString()); }
                evidence["nativeSave"] = saveProbe?.Capture(); evidence["noPreparation"] = restoration?.Capture();
                evidence["nativeLoad"] = loadProbe?.Capture();
                if (admitted) evidence["settledWorld"] = WorldSnapshot();
                var saveReleased = saveProbe == null; Exception failure = null;
                try { saveProbe?.Dispose(); saveReleased = true; } catch (Exception error) { failure = error; Fail("C7-stock-mammoth-cleanup", error.ToString()); }
                try { restoration?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-stock-mammoth-cleanup", error.ToString()); }
                if (saveReleased) try { loadProbe?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-stock-mammoth-cleanup", error.ToString()); }
                if (failure != null) throw new InvalidOperationException("Stock Mammoth observer release retains custody.", failure);
                Require(loadProbe == null || loadProbe.IdentityReleased, "Stock Mammoth native observation labels remain retained.");
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["nativeWorkerSettled"] = !Busy(),
                    ["observersReleased"] = true, ["sourceUnchanged"] = sourceUnchanged,
                    ["selectionRestored"] = !presentationCaptured || SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection),
                    ["pauseRestored"] = !presentationCaptured || game.IsPaused == originalPause,
                    ["ummLeaseReleased"] = ummLease == null, ["manualRefunds"] = 0,
                    ["manualCompanionRemovals"] = 0, ["manualFactRemovals"] = 0,
                    ["authority"] = AuthoritySnapshot() };
                evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds; settled = true; Pass("C7-stock-mammoth-cleanup");
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind,
                    errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
                completed = true;
            }
            catch (Exception error) { Fail("C7-stock-mammoth-cleanup", error.ToString()); Retain(error.Message); }
        }
        private Chunk7NativeBuilderCancellation cancellation;
        private void VerifySource()
        {
            var source = new FileInfo(sourcePath);
            Require(source.Exists && source.Length == sourceLength && source.LastWriteTimeUtc.Ticks == sourceTicks &&
                (source.Attributes & FileAttributes.ReparsePoint) == 0 && Hash(sourcePath) == request.PersistenceLoad.Sha256,
                "Stock Mammoth immutable clean-seed input changed during native fixture production.");
        }
        private void Retain(string reason)
        {
            if (pending || clock.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-stock-mammoth-cleanup", reason + " The failure deadline never relinquishes custody.");
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL",
                new JObject { ["reason"] = reason, ["cleanupCustodyRetained"] = true,
                    ["cleanupPendingAtHostBoundary"] = true, ["authority"] = AuthoritySnapshot() }, errors);
            pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-stock-mammoth-interrupted", "Stock Mammoth fixture interrupted before terminal native proof."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && settled;
        }
        public void Dispose() { if (disposed) return; if (!DrainForHost()) throw new InvalidOperationException("Stock Mammoth fixture cleanup retains custody."); disposed = true; }
    }
}
