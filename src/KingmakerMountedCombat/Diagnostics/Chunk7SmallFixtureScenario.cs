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
    // F09 fixture production only. All progression, payment, pet creation and
    // field membership use the installed native UI. No Small profile is admitted
    // or mounted here, and neither ordinary F10 producer nor its Auto is reused.
    internal sealed partial class Chunk7SmallFixtureScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-small-fixture-evidence.json";
        internal const string EvidenceKind = "chunk7-small-fixture-evidence";
        internal const string PendingFileName = "chunk7-small-fixture-cleanup-pending.json";
        internal const string PendingKind = "chunk7-small-fixture-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly WorkingFixtureLoader loader;
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly MountedPersistenceService persistence;
        private readonly HorseCompanionBlueprintService horses;
        private readonly Game game;
        private readonly SaveManager manager;
        private readonly Player menuPlayer;
        private readonly int generation;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly JArray choices = new JArray();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private readonly List<SmallInputPin> inputPins = new List<SmallInputPin>();
        private Chunk7BootstrapInventoryProbe loadProbe;
        private NativeArchiveRestorationProbe restoration;
        private Chunk7NativeWorldScope scope;
        private Chunk7OrdinaryBuildDriver mainDriver, hireDriver;
        private Chunk7NativePartyFieldAdmission field;
        private Chunk7NativeBuilderCancellation cancellation;
        private UnitEntityData main, mainHorse, actor, pet;
        private UnitEntityData[] originalCharacters, originalSelection;
        private JObject neighbourBefore, arrangementBeforeSave, bodyBeforeSave;
        private long initialMoney, nativeHireCost;
        private int nativeHireXp, stage;
        private bool begun, admitted, presentationCaptured, originalPause, cleanup, completed, settled, disposed, pending;
        private double cleanupStart;
        private MovementCaptureOverlayLease ummLease;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal static bool SupportsScenario(string scenario) => scenario == Chunk7SmallFixtureSelection.Scenario;

        internal Chunk7SmallFixtureScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, WorkingFixtureLoader loader, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            if (request?.SmallFixture == null || request.Validate().Count != 0 ||
                !Chunk7SmallFixtureSelection.IsSource(request.PersistenceLoad))
                throw new ArgumentException("An exact F09 owner-seed Small fixture request is required.");
            this.request = request; this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            game = Game.Instance ?? throw new InvalidOperationException("F09 pre-load Game is unavailable.");
            manager = game.SaveManager ?? throw new InvalidOperationException("F09 pre-load SaveManager is unavailable.");
            menuPlayer = game.Player; generation = authorization.AuthorityGeneration;
        }

        internal void BeginBeforeLoad()
        {
            Require(!begun && !cleanup && !disposed, "F09 observers may be installed only once before load."); begun = true;
            RequireAuthority(false); RequireNoOwners(); RequireNoEffects();
            Require(authority.AdmittedNativeLoadRequests == 0 && authorization.AuthorizedLoadCount == 0 &&
                loader.State == WorkingFixtureLoadState.NotStarted && !loader.LoadCallbackObserved && loader.Descriptor == null &&
                game.CurrentlyLoadedArea == null && ReferenceEquals(game.Player, menuPlayer) && menuPlayer?.MainCharacter.Value == null &&
                Game.NewGameUnit == null && !Busy() && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "F09 must own an idle empty menu before its sole native seed load.");
            ReadInputs();
            loadProbe = new Chunk7BootstrapInventoryProbe(game); loadProbe.Install();
            restoration = new NativeArchiveRestorationProbe(persistence); restoration.Install();
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(request.SmallFixture);
            evidence["contract"] = "owner-seed-unmounted-small-fixture-v1";
            evidence["fixtureSetupOnly"] = true; evidence["producerAncestry"] = false;
            evidence["profileAdmissionChanged"] = false; evidence["poseQualified"] = false;
            evidence["mountedBehaviorQualified"] = false; evidence["coldLoadQualified"] = false;
            evidence["xpWrites"] = 0; evidence["moneyWrites"] = 0; evidence["resourceWrites"] = 0;
            evidence["mountRequests"] = 0; evidence["manualPartyMutations"] = 0;
            evidence["choices"] = choices; evidence["beforeLoadAuthority"] = AuthoritySnapshot();
            Pass("C7-small-fixture-source");
        }

        internal void Update()
        {
            if (completed || disposed) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                Require(begun && clock.Elapsed.TotalSeconds <= 720, "F09 fixture exceeded its bounded native setup window.");
                RequireAuthority(false);
                Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown, "Unexpected dialog blocks F09 native setup.");
                if (Busy()) return;
                RequireNoOwners(); RequireNoEffects();
                if (admitted) RequireRoster();
                switch (stage)
                {
                    case 0: BindColdWorld(); break;
                    case 1:
                        if (mainDriver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                        mainHorse = RequireHorse(main); scope.Track(mainHorse);
                        evidence["mainBuild"] = mainDriver.CaptureState();
                        evidence["mainNativeCommitCount"] = mainDriver.NativeCommitCount;
                        evidence["mainAutoLevelupOverrideCount"] = mainDriver.AutoLevelupOverrideCount;
                        Require(mainDriver.NativeCommitCount == 1 && mainDriver.AutoLevelupOverrideCount == 0 && game.Player.Money == initialMoney,
                            "Main native Ranger 3 to 4 must commit exactly once without money or plan overrides.");
                        stage = 2; break;
                    case 2:
                        if (!Ready(mainHorse)) return;
                        BeginPaidHire(); break;
                    case 3:
                        if (hireDriver.Actor != null && actor == null) { actor = hireDriver.Actor; scope.Track(actor); }
                        if (hireDriver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                        actor = hireDriver.Actor; pet = RequireHorse(actor); scope.Track(actor); scope.Track(pet);
                        Require(hireDriver.NativeCommitCount == 4 && hireDriver.AutoLevelupOverrideCount == 0 &&
                            actor.Descriptor.Progression.Experience == nativeHireXp && game.Player.Money == initialMoney - nativeHireCost,
                            "Paid native hire must commit exactly four levels with its inherited XP and once-only actual cost.");
                        evidence["paidBuild"] = hireDriver.CaptureState(); evidence["afterBuildArrangement"] = Arrangement();
                        evidence["paidNativeCommitCount"] = hireDriver.NativeCommitCount;
                        evidence["paidAutoLevelupOverrideCount"] = hireDriver.AutoLevelupOverrideCount;
                        Pass("C7-small-fixture-native-choices"); stage = 4; break;
                    case 4:
                        RequireWorld(); RequireNeighbours();
                        Require(game.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                            !game.Player.PartyCharacters.Any(value => ReferenceEquals(value.Value, actor)),
                            "Paid native companion must be the exact remote actor before field admission.");
                        Require(game.CurrentMode == GameModeType.Default && !game.CutsceneLock && !game.Player.IsInCombat,
                            "F09 party selection requires native idle exploration.");
                        field = new Chunk7NativePartyFieldAdmission(actor, originalCharacters.Select(value => value.UniqueId).ToArray(),
                            () => { RequireAuthority(cleanup); RequireWorld(); RequireNoOwners(); });
                        stage = 5; field.Open(); break;
                    case 5:
                        if (!field.Completed && !field.Tick()) return;
                        if (!Ready(actor) || !Ready(pet)) return;
                        if (NativeSmallRiderBody.Capture(actor, pet)?.NativeRigAvailable != true) return;
                        CaptureUnmountedFixture(); BeginManual(); break;
                    case 6: PollManual(); break;
                    default: throw new InvalidOperationException("Unknown F09 fixture phase.");
                }
            }
            catch (Exception error) { Fail("C7-small-fixture-runtime", error.ToString()); BeginCleanup(); }
        }

        private void BindColdWorld()
        {
            if (loader.State != WorkingFixtureLoadState.LoadedAndVerified || !loader.LoadCallbackObserved || loadProbe?.Complete != true) return;
            Require(authority.AdmittedNativeLoadRequests == 1 && authorization.AuthorizedLoadCount == 1 &&
                ReferenceEquals(loadProbe.ObservedPlayer, game.Player) && loader.Descriptor != null &&
                loader.Descriptor.FileName == request.PersistenceLoad.FileName &&
                authority.ValidateArchiveIdentity(Target(loader.Descriptor), authority.Root) == null,
                "F09 source did not complete its exact native load and verified descriptor.");
            if (game.CurrentMode != GameModeType.Default || game.CutsceneLock || game.IsModeActive(GameModeType.Dialog) ||
                horses.State != HorseCompanionBlueprintState.Registered) return;
            Require(game.Player != null && game.State?.LoadedAreaState?.MainState != null && !game.Player.IsInCombat &&
                game.Player.GameId == Chunk7SmallFixtureSelection.CampaignId &&
                game.Player.MainCharacter.Value?.UniqueId == Chunk7SmallFixtureSelection.MainActorId &&
                game.Player.MainCharacter.Value.CharacterName == Chunk7SmallFixtureSelection.CampaignName &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == Chunk7SmallFixtureSelection.AreaId &&
                persistence.LoadedArchive == null && persistence.LoadedArchiveKind == MountedSaveReadKind.Missing && !persistence.LoadedArchiveCanRestore,
                "F09 initial source is not the exact clean owner campaign on the missing-metadata path.");
            main = game.Player.MainCharacter.Value;
            originalCharacters = game.Player.PartyCharacters.Take(7).Select(value => value.Value).ToArray();
            var ids = new[] { Chunk7SmallFixtureSelection.MainActorId, "5dd7bd1d-ae4c-4df7-9687-50fceb027344",
                "8265f8cf-0969-4ec7-a266-2448de91c8c4", "fd213210-71cd-41b0-b35b-68e1b18e9b43" };
            Require(originalCharacters.Length == 4 && originalCharacters.All(value => Ready(value) && value.Descriptor.Pet == null) &&
                originalCharacters.Select(value => value.UniqueId).OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(ids.OrderBy(value => value, StringComparer.Ordinal)) &&
                !game.Player.RemoteCompanions.Any() && Chunk7NativeFieldRoster.Capture(game.Player).Length == 4,
                "F09 clean seed must retain its exact four petless field characters and no remote companions.");
            scope = new Chunk7NativeWorldScope(request, originalCharacters); admitted = true;
            neighbourBefore = CaptureNeighbours(); initialMoney = game.Player.Money;
            Require(initialMoney == qualifiedSeedMoney, "F09 native seed money differs from its accepted cold qualification.");
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            Require(ranger != null && horses.HorseFeature != null && horses.LevelRank != null && horses.HorseUnit != null,
                "F09 exact native Ranger and registered KMC Horse contracts are unavailable.");
            Chunk7SmallFixtureAdmission.RequireMain(main.Descriptor.Progression.CharacterLevel, main.Descriptor.Progression.GetClassLevel(ranger),
                main.Descriptor.Progression.Experience, ReadyLevel(main.Descriptor.Progression.Experience), LevelUpController.CanLevelUp(main.Descriptor),
                main.Descriptor.Pet != null, initialMoney, game.Player.GetCustomCompanionCost(), ReadyLevel(BlueprintRoot.Instance.Progression.XPTable.GetBonus(4)));
            Require(restoration.Complete && restoration.Capture()["events"] is JArray restoreEvents && restoreEvents.Count == 0,
                "F09 missing-metadata source must cause no native preparation or KMC actor restoration.");
            evidence["loadedWorld"] = WorldSnapshot(); evidence["neighboursBefore"] = neighbourBefore.DeepClone();
            evidence["loadedArchiveKind"] = "Missing"; evidence["loadedArchiveCanRestore"] = false;
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray();
            Require(originalSelection.Distinct().Count() == originalSelection.Length && originalSelection.All(value => originalCharacters.Contains(value)),
                "Initial F09 selection is foreign or duplicate.");
            originalPause = game.IsPaused; presentationCaptured = true;
            MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            game.IsPaused = false;
            mainDriver = new Chunk7OrdinaryBuildDriver(new Chunk7OrdinaryBuildPins(request.RunId, request.SmallFixture.NativeInventoryArtifactSha256,
                Chunk7OrdinaryBuildMode.ExistingRangerLevels, null, null, null, Gender.Male, false, 4, Size.Medium), main,
                horses.HorseFeature, horses.HorseUnit.AssetGuid, RequireChoiceAuthority, ObserveChoice);
            stage = 1; Pass("C7-small-fixture-native-load");
        }

        private void BeginPaidHire()
        {
            RequireChoiceAuthority(); RequireHorse(main);
            nativeHireCost = game.Player.GetCustomCompanionCost(); nativeHireXp = BlueprintRoot.Instance.Progression.XPTable.GetBonus(main.Descriptor.Progression.CharacterLevel);
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            Chunk7SmallFixtureAdmission.RequireHire(main.Descriptor.Progression.CharacterLevel, main.Descriptor.Progression.GetClassLevel(ranger),
                main.Descriptor.Progression.Experience, LevelUpController.CanLevelUp(main.Descriptor), game.Player.Money,
                nativeHireCost, nativeHireXp, ReadyLevel(nativeHireXp));
            Require(game.Player.Money == initialMoney, "Main progression changed native money before paid hire.");
            evidence["paidHireAdmission"] = new JObject { ["moneyBefore"] = initialMoney, ["nativeCost"] = nativeHireCost,
                ["nativeInheritedXp"] = nativeHireXp, ["nativeReadyLevel"] = ReadyLevel(nativeHireXp), ["forFree"] = false,
                ["matchPlayerXpExactly"] = false, ["importableInDungeon"] = false, ["xpAssistance"] = 0, ["moneyAssistance"] = 0 };
            hireDriver = new Chunk7OrdinaryBuildDriver(new Chunk7OrdinaryBuildPins(request.RunId, request.SmallFixture.NativeInventoryArtifactSha256,
                Chunk7OrdinaryBuildMode.NewSmallCustomCompanion, Chunk7SmallFixtureSelection.RaceGuid, Chunk7SmallFixtureSelection.PresetGuid,
                Chunk7SmallFixtureSelection.SkeletonName, Gender.Male, false, 4, Size.Small), null,
                horses.HorseFeature, horses.HorseUnit.AssetGuid, RequireChoiceAuthority, ObserveChoice);
            stage = 3;
        }
        private void RequireChoiceAuthority()
        {
            Require(!cleanup && (stage == 1 || stage == 2 || stage == 3), "F09 native builder is outside its closed setup phase.");
            RequireAuthority(false); RequireWorld(); RequireRoster(); RequireNeighbours(); RequireNoOwners(); RequireNoEffects();
            Require(!Busy() && !game.Player.IsInCombat && !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog) &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown) && !NativePersistenceIsolation.NativeSlotRotationEnabled,
                "F09 builder requires its exact idle world, no dialog, no worker and no slot projection.");
        }
        private void ObserveChoice(JObject row)
        { Require(choices.Count < 1400, "F09 native choices exceeded their bounded evidence capacity."); choices.Add(row.DeepClone()); }
        private UnitEntityData RequireHorse(UnitEntityData rider)
        {
            var horse = rider?.Descriptor.Pet; var fact = rider?.Descriptor.GetFact(horses.HorseFeature) as Feature;
            Require(rider != null && rider.Descriptor.Progression.CharacterLevel == 4 &&
                horse != null && ReferenceEquals(horse.Blueprint, horses.HorseUnit) && ReferenceEquals(horse.Descriptor.Master.Value, rider) &&
                fact?.Source != null && (rider.Descriptor.GetFact(horses.LevelRank)?.GetRank() ?? 0) > 0,
                "F09 native Ranger completion did not produce its sourced reciprocal registered Horse."); return horse;
        }
        private static int ReadyLevel(int xp)
        { var level = 0; while (level < 20 && BlueprintRoot.Instance.Progression.XPTable.GetBonus(level + 1) <= xp) level++; return level; }
        private static bool Ready(UnitEntityData value) => value != null && value.IsInState && value.IsInGame && value.View?.AgentASP != null &&
            value.Descriptor.State.IsConscious && !value.Descriptor.State.IsDead && !value.IsInCombat && value.Commands != null && value.Commands.Empty &&
            value.Commands.Queue != null && value.Commands.Queue.Count == 0;
        private bool Busy() => LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private void RequireAuthority(bool cleanupOnly)
        {
            var expectedRoot = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)), "runtime-staging", "persistence-" + request.RunId, "Saved Games");
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager) && authorization.IsActive &&
                authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) && NativePersistenceIsolation.IsExactAuthority(authority) &&
                authority.Root == expectedRoot && manager.SavePath == expectedRoot &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(expectedRoot), "Areas") &&
                authority.MatchesSmallFixturePlan(request, game, manager) && authority.AdmittedNativeLoadRequests <= 1 && authority.AdmittedNativeWriteRequests <= 1 &&
                authorization.AuthorizedLoadCount == authority.AdmittedNativeLoadRequests && authorization.AuthorizedWriteCount == authority.AdmittedNativeWriteRequests &&
                !authorization.IsOneShotWorkingWriteSuppressionArmed, "F09 lost its exact isolated one-load one-Manual authority.");
            if (!cleanupOnly) Require(authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 &&
                authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 && authorization.BaselineLoadRequestCount == 0,
                "F09 encountered a refused or undeclared native boundary.");
        }
        private void RequireWorld() { Require(admitted && scope != null, "F09 has not admitted a native world."); scope.RequireCurrent(); }
        private void RequireRoster()
        {
            RequireWorld();
            var characters = game.Player.PartyCharacters.Take(7).Select(value => value.Value).ToArray();
            var remote = game.Player.RemoteCompanions.Take(3).Select(value => value.Value).ToArray();
            var paid = actor ?? hireDriver?.Actor;
            Require(characters.Length == 4 || field != null && characters.Length == 5,
                "F09 native character count changed outside its owned field selection.");
            Require(originalCharacters.All(value => characters.Count(other => ReferenceEquals(value, other)) == 1) &&
                characters.All(value => originalCharacters.Contains(value) || field != null && ReferenceEquals(value, paid)) &&
                remote.Length <= 1 && remote.All(value => paid != null && ReferenceEquals(value, paid)) &&
                !remote.Any(value => characters.Any(other => ReferenceEquals(other, value))),
                "F09 lost an original character or admitted an unselected/duplicate remote or field character.");
            var live = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(live.All(value => originalCharacters.Contains(value) || ReferenceEquals(value, main.Descriptor.Pet) ||
                field != null && (ReferenceEquals(value, paid) || ReferenceEquals(value, paid?.Descriptor.Pet))),
                "F09 native field graph contains an unselected actor or companion.");
            Require(originalCharacters.Where(value => !ReferenceEquals(value, main)).All(value => value.Descriptor.Pet == null),
                "F09 native setup changed an unrelated original companion's pet ownership.");
            Require(mainHorse == null || ReferenceEquals(main.Descriptor.Pet, mainHorse),
                "F09 paid hire replaced the original main character's native Horse neighbour.");
        }
        private void RequireNoOwners() => Require(world.Registry.Owners.Length == 0 && controls.CapturePersistentSlots().Length == 0 &&
            controls.NativeCastRequestCount == 0 && !controls.HasUnsettledRelationshipTransition && !world.UnifiedTurn.HasUnsettledPreparation &&
            world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.PairOwner == null &&
                pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasActiveCommand && !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership),
            "F09 fixture must remain wholly unmounted without paired commands or prepared residue.");
        private void RequireNoEffects() => Require(persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 &&
            persistence.SnapshotCount <= (saveRequested ? 1 : 0) && persistence.CompletedSaveCount <= (saveRequested ? 1 : 0) &&
            authority.AdmittedNativeWriteRequests <= (saveRequested ? 1 : 0), "F09 observed an unselected write, snapshot or mounted restore.");
        private JObject AuthoritySnapshot() => new JObject { ["root"] = authority.Root, ["generation"] = generation,
            ["maximumNativeLoads"] = authority.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests,
            ["admittedNativeLoads"] = authority.AdmittedNativeLoadRequests, ["admittedNativeWrites"] = authority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount, ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount, ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount,
            ["reservations"] = authority.SmallFixtureReservations, ["manualNumber"] = authority.SmallFixtureNumber, ["outputLeaf"] = authority.SmallFixtureOutputLeaf };
        private static RuntimeSaveTarget Target(SaveInfo save) => new RuntimeSaveTarget { InternalName = save.Name, FileName = save.FileName,
            FullPath = save.FolderName, SaveType = save.Type.ToString(), GameId = save.GameId, GameName = save.GameName, Area = save.Area?.AssetGuidThreadSafe };
        private void Pass(string row) { if (results.All(value => value.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return; errors.Add(error); var result = results.SingleOrDefault(value => value.Name == row);
            if (result == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { result.Status = "FAIL"; result.AssertionFailCount++; result.Errors = result.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }
}
