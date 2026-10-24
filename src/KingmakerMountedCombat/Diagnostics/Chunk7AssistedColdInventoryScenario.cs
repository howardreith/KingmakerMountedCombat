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
    // Observation only after one exact native cold load. The distinct fourth-
    // Horse selector case opens and closes one owned native party selector;
    // neither case issues a save, party switch, choice, purchase or resource write.
    internal sealed partial class Chunk7AssistedColdInventoryScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-assisted-cold-inventory-evidence.json";
        internal const string EvidenceKind = "chunk7-assisted-cold-inventory-evidence";
        internal const string PendingFileName = "chunk7-assisted-cold-inventory-cleanup-pending.json";
        internal const string PendingKind = "chunk7-assisted-cold-inventory-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly Chunk7AssistedColdInventorySelection selection;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly NativeMountedControlService controls;
        private readonly Game game;
        private readonly SaveManager manager;
        private readonly WorkingFixtureLoader loader;
        private readonly HorseCompanionBlueprintService horses;
        private readonly Player menuPlayer;
        private readonly int generation;
        private readonly string root;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private NativeArchiveRestorationProbe restoration;
        private Chunk7NativePartyOptionalityProbe optionality;
        private Chunk7BootstrapInventoryProbe probe;
        private Chunk7OwnerSeedLoadProbe resolution;
        private string seedRecordSha256;
        private string[] seedDlc;
        private string sourcePath, archivePath, expectedMainId;
        private long sourceLength, sourceTicks, archiveLength, archiveTicks, expectedNativeMoney;
        private JObject sourceWorld, sourceReadiness;
        private JArray thirdHorseOwners;
        private JArray thirdHorseFactsAtLoad;
        private UnitEntityData[] actors, characters, remote;
        private JObject loadedSemantics;
        private object nativePlayer, state, areaState, mainState, area, mainActor, loadDescriptor;
        private object[] descriptors, views;
        private bool begun, admitted, cleanup, completed, settled, disposed, pending, sourcePinned;
        private double cleanupStart;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal static bool SupportsScenario(string value) => value == Chunk7AssistedColdInventorySelection.Scenario;

        internal Chunk7AssistedColdInventoryScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, WorkingFixtureLoader loader,
            HorseCompanionBlueprintService horses)
        {
            if (request?.AssistedColdInventory == null || request.Validate().Count != 0)
                throw new ArgumentException("An exact closed assisted cold-inventory request is required.");
            this.request = request; selection = request.AssistedColdInventory;
            this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
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
            Require(loader.State == WorkingFixtureLoadState.NotStarted && !loader.LoadCallbackObserved && loader.Descriptor == null,
                "Assisted cold observers must own the exact loader before it starts.");
            Require(authority.AdmittedNativeLoadRequests == 0 && authorization.AuthorizedLoadCount == 0,
                "Cold observation must precede its sole native load admission.");
            Require(game.CurrentlyLoadedArea == null && ReferenceEquals(game.Player, menuPlayer) &&
                menuPlayer?.MainCharacter.Value == null && Game.NewGameUnit == null && !Busy() &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "Cold inventory must own the exact idle empty menu before load.");
            RequireNoOwners(); RequireNoEffects();
            if (selection.IsOwnerSeed) ReadOwnerSeedSource(); else if (selection.IsHorseWorld) ReadThirdHorseSource();
            else if (selection.IsPaidFixture) ReadSmallFixtureSource(); else ReadSource();
            probe = new Chunk7BootstrapInventoryProbe(game); probe.Install();
            restoration = new NativeArchiveRestorationProbe(persistence); restoration.Install();
            if (selection.IsOwnerSeed) { resolution = new Chunk7OwnerSeedLoadProbe(); resolution.Install(); }
            evidence["processId"] = Process.GetCurrentProcess().Id;
            evidence["selection"] = Chunk7EvidenceDto.Request(selection);
            if (selection.IsOwnerSeed)
                evidence["seedSource"] = new JObject { ["seedId"] = selection.SeedId, ["seedSha256"] = selection.SeedSha256,
                    ["recordSha256"] = seedRecordSha256, ["copiedRecordPath"] = sourcePath, ["archiveLeaf"] = request.PersistenceLoad.FileName,
                    ["registeredDlc"] = new JArray(seedDlc), ["ownerAuthorized"] = true, ["producerAncestry"] = false, ["diagnosticArchiveContentRead"] = true };
            else
            {
                evidence["source"] = new JObject { ["runId"] = selection.SourceRunId, ["artifactSha256"] = selection.SourceArtifactSha256,
                    ["archiveSha256"] = selection.SourceArchiveSha256, ["archiveLeaf"] = request.PersistenceLoad.FileName,
                    ["sameCandidate"] = !selection.IsHorseWorld, ["assistedSetupOnly"] = true,
                    ["copiedArtifactPath"] = sourcePath, ["diagnosticArchiveContentRead"] = false };
                evidence["sourceNativeWorld"] = sourceWorld.DeepClone();
                if (!selection.IsPaidFixture && !selection.IsHorseWorld) evidence["sourceFixtureReadiness"] = sourceReadiness.DeepClone();
                evidence["sourceNativeMoney"] = expectedNativeMoney;
                if (selection.IsPaidFixture) evidence["source"]["kind"] = selection.IsMediumFixture
                    ? "qualified-owner-seed-derived-medium-fixture-manual" : "qualified-owner-seed-derived-small-fixture-manual";
                if (selection.IsThirdHorse) evidence["source"]["kind"] = "qualified-assisted-third-horse-manual";
                if (selection.IsFourthHorseSource) evidence["source"]["kind"] = "reader-reevaluated-assisted-fourth-horse-manual";
                if (selection.IsFifthHorseSource) evidence["source"]["kind"] = "qualified-paid-fifth-horse-manual";
                if (selection.IsSixthHorse) evidence["source"]["kind"] = "reader-reevaluated-paid-sixth-horse-manual";
            }
            evidence["beforeLoad"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["game"] = probe.Identity(game),
                ["saveManager"] = probe.Identity(manager), ["menuPlayer"] = probe.Identity(menuPlayer),
                ["loadedAreaPresent"] = false, ["nativeLoading"] = false, ["authority"] = AuthoritySnapshot() };
            evidence["resourceWrites"] = 0; evidence["xpWrites"] = 0; evidence["moneyWrites"] = 0;
            evidence["profileAdmissionChanged"] = false; evidence["ordinaryAcquisitionProven"] = false;
            evidence["sixPairFixtureQualified"] = false; evidence["mountRequests"] = 0; evidence["choiceRequests"] = 0;
            evidence["ordinaryProgressionProven"] = false; Pass("C7-assisted-cold-source");
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
                Require(clock.Elapsed.TotalSeconds <= 180, "Cold inventory exceeded its bounded native load/readiness window.");
                if (Busy()) return;
                if (departure != null)
                {
                    if (!departure.Tick()) return;
                    CompleteNativeDeparture();
                    Pass("C7-assisted-cold-native-departure"); BeginCleanup(); return;
                }
                if (optionality != null)
                {
                    if (!optionality.Tick()) return;
                    evidence["selectorProbe"] = optionality.Capture();
                    Pass("C7-assisted-cold-selector"); BeginCleanup(); return;
                }
                Require(loader.State == WorkingFixtureLoadState.LoadedAndVerified && loader.LoadCallbackObserved && loader.Errors.Count == 0,
                    "Assisted cold load requires the actual one-shot loader callback and completed verification.", Chunk7OwnerSeedAssertions.NativeLoadIncomplete);
                RequireLoader();
                Require(authority.AdmittedNativeLoadRequests == 1 && authorization.AuthorizedLoadCount == 1 &&
                    probe != null && probe.Complete && ReferenceEquals(probe.ObservedPlayer, game.Player),
                    "Exact native cold load and Player.PostLoad did not complete.");
                var saved = request.PersistenceLoad;
                // An owner seed has no producer roster: bind the loaded main actor once,
                // only when it carries the registered header name; later frames must keep it.
                if (selection.IsOwnerSeed && expectedMainId == null && game.Player?.MainCharacter.Value?.CharacterName == saved.GameName)
                    expectedMainId = game.Player.MainCharacter.Value.UniqueId;
                Require(game.Player != null && game.State?.LoadedAreaState?.MainState != null &&
                    game.CurrentlyLoadedArea?.AssetGuidThreadSafe == saved.Area && game.Player.GameId == saved.GameId &&
                    game.Player.MainCharacter.Value?.CharacterName == saved.GameName &&
                    game.Player.MainCharacter.Value.UniqueId == expectedMainId,
                    "Native cold world differs from its engine-minted source identity.", Chunk7OwnerSeedAssertions.SeedIdentityMismatch);
                if (game.CurrentMode != GameModeType.Default || game.CutsceneLock || game.IsModeActive(GameModeType.Dialog) || game.Player.Dialog?.Scheduled != null) return;
                Require(!game.Player.IsInCombat, "Read-only authored inventory does not admit combat.");
                var candidateActors = Chunk7NativeFieldRoster.Capture(game.Player);
                Require(candidateActors.Length > 0 && candidateActors.Length <= 12, "Assisted cold field exceeds its twelve-actor bound.");
                var expected = selection.IsOwnerSeed ? null : (JArray)sourceWorld["party"];
                Require((expected == null || candidateActors.Length == expected.Count) && candidateActors.Select(actor => actor.UniqueId).Distinct(StringComparer.Ordinal).Count() == candidateActors.Length,
                    "Cold native party differs from its exact source roster.");
                foreach (var actor in candidateActors)
                {
                    if (expected != null)
                    {
                        var matches = expected.Where(row => (string)row["id"] == actor.UniqueId).ToArray();
                        Require(matches.Length == 1 && actor.Blueprint?.AssetGuid == (string)matches[0]["blueprint"],
                            "Cold native actor or blueprint was substituted.");
                    }
                    else Require(actor.Blueprint?.AssetGuid != null, "Owner-seed native actor has no resolved blueprint.", Chunk7OwnerSeedAssertions.SeedLostContent);
                    if (!actor.IsInState || !actor.IsInGame || actor.View?.AgentASP == null) return;
                    Require(actor.Descriptor.State.IsConscious && !actor.Descriptor.State.IsDead && !actor.IsInCombat &&
                        actor.Commands != null && actor.Commands.Empty && actor.Commands.Queue != null && actor.Commands.Queue.Count == 0,
                        "Read-only assisted cold inventory found a nonliving, combat or unsettled command actor.");
                }
                RequireNoOwners(); RequireNoEffects();
                var data = persistence.LoadedArchive;
                if (selection.IsOwnerSeed)
                    Require(data == null && persistence.LoadedArchiveKind == MountedSaveReadKind.Missing && !persistence.LoadedArchiveCanRestore,
                        "Owner seed did not take the supported missing-KMC-metadata path.", Chunk7OwnerSeedAssertions.MetadataNotMissing);
                else Require(data != null && data.SchemaVersion == 3 && data.CampaignId == saved.GameId && data.AreaId == saved.Area &&
                    data.Pairs.Length == 0 && data.Participations.Length == 0 && data.Slots.Length == 0 && data.Combat == null,
                    "Cold source is not the qualified empty mounted archive.");
                var trace = restoration.Capture();
                Require(restoration.Complete && trace["events"] is JArray entries && entries.Count == 0,
                    "Cold inventory observed native Prepare/round or KMC actor/debt restoration entries.", Chunk7OwnerSeedAssertions.NativeRestorationEntries);
                nativePlayer = game.Player; state = game.State; areaState = game.State.LoadedAreaState;
                mainState = game.State.LoadedAreaState.MainState; area = game.CurrentlyLoadedArea; mainActor = game.Player.MainCharacter.Value;
                actors = candidateActors; descriptors = actors.Select(actor => (object)actor.Descriptor).ToArray(); views = actors.Select(actor => (object)actor.View).ToArray();
                characters = game.Player.PartyCharacters.Take(7).Select(value => value.Value).ToArray();
                remote = game.Player.RemoteCompanions.Take(33).Select(value => value.Value).ToArray();
                Require(characters.Length > 0 && characters.Length <= 6 && remote.Length <= 32 && remote.All(value => value != null) &&
                    remote.Select(value => value.UniqueId).Distinct(StringComparer.Ordinal).Count() == remote.Length && remote.All(value => !actors.Contains(value)),
                    "Assisted cold native character/remote roster is malformed, overlapping or unbounded.");
                if (!selection.IsOwnerSeed) RequireSourceRoster();
                loadedSemantics = CaptureSemantics();
                if (selection.IsHorseWorld) {
                    thirdHorseFactsAtLoad = CaptureThirdHorseFacts();
                    evidence[selection.IsSixthHorse ? "sixthHorseFacts" : selection.IsFifthHorseSource ? "fifthHorseFacts" : selection.IsFourthHorseSource ? "fourthHorseFacts" : "thirdHorseFacts"] = thirdHorseFactsAtLoad.DeepClone();
                }
                if (selection.IsSixthHorse) { sixthRemoteAtLoad = CaptureSixthRemote(); evidence["remoteAtLoad"] = sixthRemoteAtLoad.DeepClone(); }
                loadDescriptor = loader.Descriptor; admitted = true; RequireWorld(); VerifySourceBytes();
                evidence["loadedWorld"] = Snapshot();
                evidence["loader"] = CaptureLoader();
                evidence["nativeProfileInventory"] = Chunk7NativeProfileInventory.Capture(actors);
                Require((string)evidence["nativeProfileInventory"]["captureStatus"] == "PASS", "Native read-only body/readiness inventory failed.");
                evidence["fixtureReadiness"] = CaptureFixtureReadiness();
                if (selection.IsOwnerSeed)
                {
                    // Observe and record; qualify only on what this load actually showed.
                    var qualification = Chunk7OwnerSeedObservation.Capture(game, actors, characters, loader.Descriptor, seedDlc, resolution, persistence, Busy(), archivePath);
                    evidence["ownerSeedQualification"] = qualification;
                    evidence["kmcMetadata"] = qualification["kmcMetadata"].DeepClone();
                    // The authored route observation belongs to the Spider Cave seed only; another
                    // registered seed area records no route claim rather than failing the load.
                    if (Chunk7StockAreaObservation.AppliesTo(game))
                        evidence["stockAreaRouteObservation"] = Chunk7StockAreaObservation.Capture(game);
                    RequireWorld();
                    Require((bool)qualification["qualified"], "Owner seed qualification failed: " +
                        string.Join(" | ", ((JArray)qualification["failures"]).Select(value => (string)value)), Chunk7OwnerSeedAssertions.Qualification);
                }
                else evidence["archive"] = new JObject { ["schemaVersion"] = data.SchemaVersion, ["campaignId"] = data.CampaignId, ["areaId"] = data.AreaId,
                    ["pairs"] = data.Pairs.Length, ["participations"] = data.Participations.Length, ["slots"] = data.Slots.Length,
                    ["combatPresent"] = data.Combat != null, ["nativeRestorationGrantedByDiagnostic"] = false };
                Pass("C7-assisted-cold-native-load"); Pass("C7-assisted-cold-readonly");
                if (selection.IsFifthHorseDeparture) StartNativeDeparture();
                else if (selection.IsFourthHorseSelector)
                {
                    var linzi = actors.Single(actor => actor.UniqueId == "8265f8cf-0969-4ec7-a266-2448de91c8c4");
                    Require(linzi.Descriptor.Pet == null && characters.Length == 5 && remote.Length == 0 &&
                        game.CurrentMode == GameModeType.Default, "Exact petless Linzi field is unavailable for the opened selector observation.");
                    optionality = new Chunk7NativePartyOptionalityProbe(linzi,
                        characters.Select(value => value.UniqueId).ToArray(), RequireSelectorAuthority,
                        () => RequireAuthority(true));
                    optionality.Open();
                }
                else BeginCleanup();
            }
            catch (Exception error) { RecordOwnerSeedFailure(error); Fail("C7-assisted-cold-runtime", error.ToString()); BeginCleanup(); }
        }
        // A failed or refused owner-seed load stays classifiable from the exact
        // observed error; the normal cleanup, restoration and settlement still run.
        private void RecordOwnerSeedFailure(Exception error)
        {
            if (!selection.IsOwnerSeed || evidence["ownerSeedLoadFailure"] != null) return;
            try
            {
                var trace = resolution?.Capture();
                var qualified = evidence["ownerSeedQualification"] != null;
                var messages = new List<string>();
                if (!qualified) messages.Add(error.GetType().FullName + ": " + error.Message);
                messages.AddRange(loader.Errors);
                if (loader.State == WorkingFixtureLoadState.Failed) messages.Add("WorkingFixtureLoader state Failed.");
                if (persistence.NativeLoadFailure != null) messages.Add(persistence.NativeLoadFailure);
                if (resolution != null) messages.AddRange(resolution.LoggedMessages);
                var harnessRefused = authorization.UnauthorizedLoadCount != 0 || authorization.UnauthorizedWriteCount != 0 ||
                    authorization.FatalViolationCount != 0 || error.Message.IndexOf("authority", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    error.Message.IndexOf("source", StringComparison.OrdinalIgnoreCase) >= 0 && error.Message.IndexOf("bytes", StringComparison.OrdinalIgnoreCase) >= 0;
                var declared = loader.Descriptor != null && File.Exists(archivePath ?? "") ? Chunk7OwnerSeedObservation.DeclaredForeignTypes(loader.Descriptor, archivePath, request.PersistenceLoad.Area) : null;
                var declaredNames = declared == null ? new string[0] : ((JArray)declared["types"]).Select(row => (string)row["type"]).ToArray();
                // KMC frame evidence, decided on FULL stacks (inner exceptions included):
                // the thrown load exception (never the harness's own assertion) and every
                // logged exception and observer fault. Stored stacks stay bounded.
                var assertion = IsHarnessAssertion(error);
                var frames = new List<string>();
                if (!assertion && !qualified) frames.AddRange(Chunk7OwnerSeedFailureClassifier.KmcFrameLines(error));
                if (resolution != null) frames.AddRange(resolution.ObservedKmcFrameLines);
                // The harness assertions that fired, by stable identifier. The qualification
                // assertion stands for each of its identified failures.
                var assertionIds = new List<string>();
                if (assertion)
                {
                    var id = error.Data[Chunk7OwnerSeedAssertions.AssertionIdKey] as string ?? Chunk7OwnerSeedAssertions.Unidentified;
                    if (id == Chunk7OwnerSeedAssertions.Qualification && qualified && evidence["ownerSeedQualification"]["failureIds"] is JArray failureIds && failureIds.Count != 0)
                        assertionIds.AddRange(failureIds.Select(value => (string)value));
                    else assertionIds.Add(id);
                }
                if (resolution != null && resolution.Faults.Count != 0 && !assertionIds.Contains(Chunk7OwnerSeedAssertions.ObserverFault))
                    assertionIds.Add(Chunk7OwnerSeedAssertions.ObserverFault);
                var named = declaredNames.Concat(resolution?.FailedBinds ?? new string[0]).ToArray();
                var blueprints = resolution?.UnresolvedBlueprints ?? new string[0];
                // Evidence only (no refusal, no assertion) is recorded alongside; the label
                // takes a runner/authority refusal first, then KMC frames, then the assertions.
                var observed = Chunk7OwnerSeedFailureClassifier.Decide(named, blueprints, messages, false, frames);
                var decision = Chunk7OwnerSeedFailureClassifier.Decide(named, blueprints, messages, harnessRefused, frames, assertionIds);
                evidence["ownerSeedLoadFailure"] = new JObject {
                    ["exceptionType"] = error.GetType().FullName, ["message"] = error.Message.Length > 4096 ? error.Message.Substring(0, 4096) : error.Message,
                    ["stack"] = Chunk7OwnerSeedFailureClassifier.BoundStack(error.StackTrace), ["harnessAssertion"] = assertion,
                    ["assertionIds"] = new JArray(assertionIds), ["kmcFrame"] = frames.Count != 0, ["kmcFrameLines"] = new JArray(frames.Take(32)),
                    ["observedEvidenceClassification"] = observed.Label, ["decidedBy"] = decision.DecidedBy,
                    ["loaderState"] = loader.State.ToString(), ["loaderErrors"] = new JArray(loader.Errors), ["loaderCallbackObserved"] = loader.LoadCallbackObserved,
                    ["postLoadEntered"] = probe != null && (int)probe.Capture()["entries"] > 0, ["postLoadComplete"] = probe != null && probe.Complete,
                    ["nativeLoadFailureCount"] = persistence.NativeLoadFailureCount, ["nativeLoadFailure"] = persistence.NativeLoadFailure,
                    ["loadObservation"] = trace, ["declaredForeignTypes"] = declared, ["harnessRefused"] = harnessRefused,
                    ["qualificationRecorded"] = qualified,
                    ["classification"] = decision.Label };
            }
            catch (Exception capture) { evidence["ownerSeedLoadFailure"] = new JObject { ["exceptionType"] = error.GetType().FullName, ["message"] = error.Message,
                ["stack"] = Chunk7OwnerSeedFailureClassifier.BoundStack(error.StackTrace),
                ["captureError"] = capture.GetType().FullName + ": " + capture.Message, ["classification"] = Chunk7OwnerSeedFailureClassifier.Unclassified }; }
        }
        // Observations recorded after the qualification (or load-failure) snapshot, while
        // the observer stayed installed during cleanup: visible, never dropped. A late
        // observer fault fails the artifact; a late game-logged exception is recorded.
        private void RecordOwnerSeedPostQualification()
        {
            if (!selection.IsOwnerSeed || resolution == null || evidence["ownerSeedPostQualification"] != null) return;
            var qualificationTrace = evidence["ownerSeedQualification"]?["unresolvedContent"]?["blueprintResolution"];
            var baseline = qualificationTrace ?? evidence["ownerSeedLoadFailure"]?["loadObservation"];
            if (baseline != null && baseline.Type != JTokenType.Object) baseline = null;
            var final = resolution.Capture();
            int Count(JToken row, string key) => row?[key]?.Type == JTokenType.Integer ? (int)row[key] : 0;
            JArray Rows(JToken row, string key) => row?[key] as JArray ?? new JArray();
            var faultsAfter = Math.Max(0, Count(final, "observerFaultCount") - Count(baseline, "observerFaultCount"));
            evidence["ownerSeedPostQualification"] = new JObject {
                ["baseline"] = baseline == null ? "none" : qualificationTrace != null ? "qualification" : "load-failure",
                ["loggedExceptionCount"] = Math.Max(0, Count(final, "loggedExceptionCount") - Count(baseline, "loggedExceptionCount")),
                ["loggedExceptions"] = new JArray(Rows(final, "loggedExceptions").Skip(Rows(baseline, "loggedExceptions").Count).Select(value => value.DeepClone())),
                ["observerFaultCount"] = faultsAfter,
                ["observerFaults"] = new JArray(Rows(final, "observerFaults").Skip(Rows(baseline, "observerFaults").Count).Select(value => value.DeepClone())) };
            if (faultsAfter != 0) Fail("C7-assisted-cold-cleanup", Chunk7OwnerSeedObserverFaults.Reason + " after qualification: " + faultsAfter + " observation hook fault(s).");
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
            result["staticPartyFacts"] = new JObject { ["selectorOpened"] = false, ["liveSelectorRequiredAuthorityAvailable"] = false,
                ["membershipOrRemovalQualified"] = false, ["observationScope"] = "static-getters-only", ["characters"] = new JArray(characters.Select(value => new JObject {
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
                saves[0].Area?.AssetGuidThreadSafe == request.PersistenceLoad.Area && saves[0].Type == SaveInfo.SaveType.Manual &&
                !saves[0].IsAutoLevelupSave && saves[0].HasFileOnDisk && saves[0].OperationState == SaveInfo.StateType.None,
                "Read-only isolated descriptor inventory differs from its sole qualified assisted Manual source.");
            Require(ReferenceEquals(settings, Kingmaker.UI.SettingsUI.SettingsRoot.Instance) && ReferenceEquals(onlyOne, settings.OnlyOneSave) &&
                ReferenceEquals(enabled, settings.AutosaveEnabled) && ReferenceEquals(slots, settings.AutosaveSlots) &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled && slots.OverrideDefaultValue == null,
                "Native settings identity or read-only getter admission changed during observation.");
            RequireWorld(); result["captureStatus"] = "PASS"; return result;
        }
        private void ReadSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7AssistedColdInventorySelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == request.PersistenceLoad.Length && archive.LastWriteTimeUtc.Ticks == request.PersistenceLoad.LastWriteTimeUtcTicks && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Exact staged assisted source/archive bytes differ.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath)); var observed = artifact["observations"];
            var saved = request.PersistenceLoad; var manual = observed?["finalManual"];
            Require((string)artifact["runId"] == selection.SourceRunId && (string)artifact["scenario"] == Chunk7AssistedFixtureSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7AssistedFixtureScenario.EvidenceKind && (string)artifact["status"] == "PASS" &&
                artifact["errors"] is JArray sourceErrors && sourceErrors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 && (string)artifact["dllMvid"] == request.DllMvid &&
                observed?["processId"]?.Type == JTokenType.Integer && (int)observed["processId"] > 0 && (int)observed["processId"] != Process.GetCurrentProcess().Id &&
                observed?["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)observed["selection"]["baseGameOnly"] &&
                observed?["selection"]?["assistedSetupOnly"]?.Type == JTokenType.Boolean && (bool)observed["selection"]["assistedSetupOnly"] &&
                (string)observed["selection"]["case"] == Chunk7AssistedFixtureSelection.CaseName &&
                (string)manual?["sha256"] == selection.SourceArchiveSha256 && (string)manual?["fileName"] == saved.FileName &&
                (string)manual?["internalName"] == saved.InternalName && (string)manual?["gameId"] == saved.GameId &&
                (string)manual?["gameName"] == saved.GameName && (string)manual?["area"] == saved.Area && (string)manual?["type"] == "Manual" &&
                (string)manual?["operation"] == "None" && manual?["autoLevelup"]?.Type == JTokenType.Boolean && !(bool)manual["autoLevelup"] &&
                (long)manual["length"] == saved.Length && (long)manual["lastWriteTimeUtcTicks"] == saved.LastWriteTimeUtcTicks,
                "Staged source lacks exact same-candidate completed assisted final Manual provenance.");
            sourceWorld = observed["settledWorld"] as JObject; sourceReadiness = observed["fixtureReadiness"] as JObject;
            Require(sourceWorld != null && sourceWorld["party"] is JArray party && party.Count > 0 && party.Count <= 12 &&
                (string)sourceWorld["gameId"] == saved.GameId && (string)sourceWorld["gameName"] == saved.GameName &&
                (string)sourceWorld["area"] == saved.Area && (string)sourceWorld["mode"] == "Default" &&
                sourceReadiness != null && (string)sourceReadiness["captureStatus"] == "PASS" &&
                sourceReadiness["staticPartyFacts"]?["characters"] is JArray sourceCharacters && sourceCharacters.Count > 0 && sourceCharacters.Count <= 6,
                "Source assisted native world/readiness is missing or unbounded.");
            var sourceInventory = observed["nativeProfileInventory"];
            Require((string)sourceInventory?["captureStatus"] == "PASS" &&
                sourceInventory?["ordinaryAcquisitionPrerequisites"]?["nativeMoney"]?.Type == JTokenType.Integer,
                "Source assisted final native funds observation is missing.");
            expectedNativeMoney = (long)sourceInventory["ordinaryAcquisitionPrerequisites"]["nativeMoney"];
            expectedMainId = (string)sourceWorld["mainActorId"];
            Require(Guid.TryParseExact(expectedMainId,"D",out var mainId) && mainId != Guid.Empty &&
                ((JArray)sourceWorld["party"]).Count(row => (string)row["id"] == expectedMainId) == 1 &&
                (string)sourceReadiness["mainProgression"]?["actorId"] == expectedMainId,
                "Source main actor lacks its exact native field and progression identity.");
            VerifySourceBytes();
        }
        private void ReadSmallFixtureSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7AssistedColdInventorySelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath); var saved = request.PersistenceLoad;
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == saved.Length && archive.LastWriteTimeUtc.Ticks == saved.LastWriteTimeUtcTicks && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Exact staged F09 Manual source/archive bytes differ.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath)); var observed = artifact["observations"];
            var manual = observed?["nativeWrite"];
            Require((string)artifact["runId"] == selection.SourceRunId && (string)artifact["scenario"] == Chunk7SmallFixtureSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7SmallFixtureScenario.EvidenceKind && (string)artifact["status"] == "PASS" &&
                artifact["errors"] is JArray errors && errors.Count == 0 &&
                (string)artifact["commit"] == request.Commit && (string)artifact["dllSha256"] == request.DllSha256 && (string)artifact["dllMvid"] == request.DllMvid &&
                observed?["processId"]?.Type == JTokenType.Integer && (int)observed["processId"] > 0 && (int)observed["processId"] != Process.GetCurrentProcess().Id &&
                (string)observed["contract"] == (selection.IsMediumFixture ? "owner-seed-unmounted-medium-fixture-v1" : "owner-seed-unmounted-small-fixture-v1") &&
                (string)observed["selection"]?["case"] == (selection.IsMediumFixture ? Chunk7SmallFixtureSelection.MediumCaseName : Chunk7SmallFixtureSelection.CaseName) &&
                observed["fixtureSetupOnly"]?.Type == JTokenType.Boolean && (bool)observed["fixtureSetupOnly"] &&
                observed["producerAncestry"]?.Type == JTokenType.Boolean && !(bool)observed["producerAncestry"] &&
                (string)manual?["sha256"] == selection.SourceArchiveSha256 && (string)manual?["fileName"] == saved.FileName &&
                (string)manual?["internalName"] == saved.InternalName && (string)manual?["gameId"] == saved.GameId &&
                (string)manual?["gameName"] == saved.GameName && (string)manual?["area"] == saved.Area && (string)manual?["type"] == "Manual" &&
                (string)manual?["operationState"] == "None" && manual?["autoLevelup"]?.Type == JTokenType.Boolean && !(bool)manual["autoLevelup"] &&
                (long)manual["length"] == saved.Length && (long)manual["lastWriteTimeUtcTicks"] == saved.LastWriteTimeUtcTicks,
                "Staged F09 source lacks exact same-candidate completed native Manual provenance.");
            sourceWorld = observed["savedWorld"] as JObject;
            var arrangement = observed["afterSaveArrangement"] as JObject;
            var party = sourceWorld?["party"] as JArray;
            var characters = arrangement?["characters"] as JArray;
            var actorRows = arrangement?["actors"] as JArray;
            Require(sourceWorld != null && party != null && party.Count == 7 &&
                (string)sourceWorld["gameId"] == saved.GameId && (string)sourceWorld["gameName"] == saved.GameName &&
                (string)sourceWorld["area"] == saved.Area && (string)sourceWorld["mode"] == "Default" &&
                arrangement != null && actorRows != null && actorRows.Count == 7 &&
                characters != null && characters.Count == 5 &&
                arrangement["remote"] is JArray remoteRows && remoteRows.Count == 0 &&
                arrangement["money"]?.Type == JTokenType.Integer && observed["acquiredOwners"] is JArray owners && owners.Count == 2,
                "F09 source native saved world or paid-Halfling arrangement is missing or unbounded.");
            sourceReadiness = arrangement;
            expectedNativeMoney = (long)arrangement["money"];
            expectedMainId = (string)sourceWorld["mainActorId"];
            Require(party.Select(row => (string)row["id"]).Distinct(StringComparer.Ordinal).Count() == 7 &&
                actorRows.Select(row => (string)row["id"]).Distinct(StringComparer.Ordinal).Count() == 7 &&
                party.All(row => actorRows.Count(actor => (string)actor["id"] == (string)row["id"] &&
                    (string)actor["blueprint"] == (string)row["blueprint"]) == 1) &&
                characters.Select(row => (string)row).Distinct(StringComparer.Ordinal).Count() == 5 &&
                characters.All(row => actorRows.Count(actor => (string)actor["id"] == (string)row) == 1),
                "F09 saved world and native arrangement disagree on their exact seven actors or five characters.");
            Require(expectedMainId == Chunk7SmallFixtureSelection.MainActorId &&
                party.Count(row => (string)row["id"] == expectedMainId) == 1 &&
                characters.Count(row => (string)row == expectedMainId) == 1,
                "F09 source main actor lacks its exact native field identity.");
            VerifySourceBytes();
        }
        private void RequireLoader()
        {
            var descriptor=loader.Descriptor; var saved=request.PersistenceLoad;
            Require(loader.State==WorkingFixtureLoadState.LoadedAndVerified && loader.LoadCallbackObserved && loader.Errors.Count==0 &&
                descriptor!=null && loader.WorkingPath==archivePath && descriptor.FileName==saved.FileName && descriptor.Name==saved.InternalName &&
                descriptor.GameId==saved.GameId && descriptor.GameName==saved.GameName && descriptor.Area?.AssetGuidThreadSafe==saved.Area &&
                descriptor.Type==SaveInfo.SaveType.Manual && !descriptor.IsAutoLevelupSave && descriptor.IsActuallySaved && descriptor.HasFileOnDisk &&
                descriptor.OperationState==SaveInfo.StateType.None && (loadDescriptor==null || ReferenceEquals(loadDescriptor,descriptor)),
                "Assisted cold loader or exact Manual descriptor changed.");
        }
        private JObject CaptureLoader()
        {
            RequireLoader(); var descriptor=loader.Descriptor;
            return new JObject { ["state"]=loader.State.ToString(), ["callbackObserved"]=loader.LoadCallbackObserved,
                ["errors"]=new JArray(loader.Errors), ["path"]=loader.WorkingPath, ["descriptor"]=probe.Identity(descriptor),
                ["fileName"]=descriptor.FileName, ["internalName"]=descriptor.Name, ["gameId"]=descriptor.GameId,
                ["gameName"]=descriptor.GameName, ["area"]=descriptor.Area.AssetGuidThreadSafe, ["type"]=descriptor.Type.ToString(),
                ["operation"]=descriptor.OperationState.ToString(), ["autoLevelup"]=descriptor.IsAutoLevelupSave,
                ["isActuallySaved"]=descriptor.IsActuallySaved, ["hasFileOnDisk"]=descriptor.HasFileOnDisk, ["authority"]=AuthoritySnapshot() };
        }
        private static JArray CaptureClasses(UnitEntityData actor)
        {
            var classes=actor.Descriptor.Progression.Classes.Take(21).ToArray();
            Require(classes.Length<=20 && actor.Descriptor.Progression.CharacterLevel>=0 && actor.Descriptor.Progression.CharacterLevel<=20 && classes.All(value=>value?.CharacterClass!=null && value.Level>0 && value.Level<=20) &&
                classes.Select(value=>value.CharacterClass.AssetGuid).Distinct(StringComparer.Ordinal).Count()==classes.Length &&
                classes.Sum(value=>value.Level)==actor.Descriptor.Progression.CharacterLevel, "Assisted cold actor progression is malformed or unbounded.");
            return new JArray(classes.OrderBy(value=>value.CharacterClass.AssetGuid,StringComparer.Ordinal).Select(value=>
                new JObject{["guid"]=value.CharacterClass.AssetGuid,["level"]=value.Level}));
        }
        private JObject CaptureSemantics() => new JObject { ["nativeMoney"] = game.Player.Money, ["actors"]=new JArray(actors.OrderBy(value=>value.UniqueId,StringComparer.Ordinal).Select(value=>new JObject {
            ["id"]=value.UniqueId,["blueprint"]=value.Blueprint.AssetGuid,["petId"]=value.Descriptor.Pet?.UniqueId,["masterId"]=value.Descriptor.Master.Value?.UniqueId,
            ["level"]=value.Descriptor.Progression.CharacterLevel,["experience"]=value.Descriptor.Progression.Experience,["classes"]=CaptureClasses(value)})) };
        private void RequireSourceRoster()
        {
            if (selection.IsHorseWorld) { RequireThirdHorseSourceRoster(); return; }
            if (selection.IsPaidFixture) { RequireSmallFixtureSourceRoster(); return; }
            Require(game.Player.Money == expectedNativeMoney, "Assisted cold native funds differ from the qualified source observation.");
            var sourceCharacters=(JArray)sourceReadiness["staticPartyFacts"]["characters"];
            Require(characters.Length==sourceCharacters.Count && characters.All(actor=>sourceCharacters.Count(row=>(string)row["actorId"]==actor.UniqueId &&
                (string)row["blueprintGuid"]==actor.Blueprint.AssetGuid)==1), "Assisted cold raw character membership differs from its qualified source.");
            foreach(var actor in actors)
            {
                var source=((JArray)sourceWorld["party"]).Single(row=>(string)row["id"]==actor.UniqueId);
                var expectedExperience=actor.UniqueId==expectedMainId ? (int)sourceReadiness["mainProgression"]["experience"] : (int)source["experience"];
                Require((string)source["blueprint"]==actor.Blueprint.AssetGuid && (string)source["petId"]==actor.Descriptor.Pet?.UniqueId &&
                    (string)source["masterId"]==actor.Descriptor.Master.Value?.UniqueId && (int)source["characterLevel"]==actor.Descriptor.Progression.CharacterLevel &&
                    expectedExperience==actor.Descriptor.Progression.Experience && JToken.DeepEquals(source["classes"],CaptureClasses(actor)),
                    "Assisted cold actor/reciprocity/progression differs from its qualified source.");
            }
        }
        private void RequireSmallFixtureSourceRoster()
        {
            Require(game.Player.Money == expectedNativeMoney, "Cold F09 native funds differ from the paid producer Manual.");
            var expectedCharacters = ((JArray)sourceReadiness["characters"]).Select(value => (string)value).ToArray();
            Require(characters.Select(value => value.UniqueId).SequenceEqual(expectedCharacters) && remote.Length == 0 && actors.Length == 7,
                "Cold F09 native character or remote membership differs from its producer Manual.");
            foreach (var actor in actors)
            {
                var source = ((JArray)sourceReadiness["actors"]).Single(row => (string)row["id"] == actor.UniqueId);
                Require((string)source["blueprint"] == actor.Blueprint.AssetGuid &&
                    (string)source["petId"] == actor.Descriptor.Pet?.UniqueId &&
                    (string)source["masterId"] == actor.Descriptor.Master.Value?.UniqueId &&
                    (int)source["level"] == actor.Descriptor.Progression.CharacterLevel &&
                    (int)source["experience"] == actor.Descriptor.Progression.Experience &&
                    JToken.DeepEquals(source["classes"], CaptureClasses(actor)),
                    "Cold F09 actor/reciprocal Horse/progression differs from its producer Manual.");
            }
        }
        private void VerifySourceBytes()
        {
            if (!sourcePinned) return;
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && (source.Attributes & FileAttributes.ReparsePoint) == 0 && source.Length == sourceLength && source.LastWriteTimeUtc.Ticks == sourceTicks &&
                archive.Exists && (archive.Attributes & FileAttributes.ReparsePoint) == 0 && archive.Length == archiveLength && archive.LastWriteTimeUtc.Ticks == archiveTicks &&
                Hash(sourcePath) == (selection.IsOwnerSeed ? seedRecordSha256 : selection.SourceArtifactSha256) &&
                Hash(archivePath) == (selection.IsOwnerSeed ? selection.SeedSha256 : selection.SourceArchiveSha256),
                "Read-only bootstrap source changed during native observation.");
        }
        // The harness writes an honest seed-source record (registry entry plus intake
        // reference) beside the profile; there is no producer artifact for an owner seed.
        private void ReadOwnerSeedSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7AssistedColdInventorySelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath); var saved = request.PersistenceLoad;
            Require(source.Exists && source.Length > 0 && source.Length <= 64 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == saved.Length && archive.LastWriteTimeUtc.Ticks == saved.LastWriteTimeUtcTicks && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(archivePath) == selection.SeedSha256, "Exact staged owner-seed record/archive bytes differ.");
            seedRecordSha256 = Hash(sourcePath);
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var record = JObject.Parse(File.ReadAllText(sourcePath)); var entry = record["registryEntry"] as JObject;
            Require((string)record["kind"] == "kmc-owner-seed-source" && record["schemaVersion"]?.Type == JTokenType.Integer && (int)record["schemaVersion"] == 1 &&
                (string)record["seedId"] == selection.SeedId && record["producerAncestry"]?.Type == JTokenType.Boolean && !(bool)record["producerAncestry"] &&
                entry != null && (string)entry["seedId"] == selection.SeedId && (string)entry["sha256"] == selection.SeedSha256 &&
                entry["length"]?.Type == JTokenType.Integer && (long)entry["length"] == saved.Length &&
                (string)entry["internalName"] == saved.InternalName && (string)entry["fileName"] == saved.FileName && (string)entry["type"] == "Manual" &&
                (string)entry["gameId"] == saved.GameId && (string)entry["gameName"] == saved.GameName && (string)entry["area"] == saved.Area &&
                entry["compatibilityVersion"]?.Type == JTokenType.Integer && (int)entry["compatibilityVersion"] == 1 &&
                entry["dlc"] is JArray dlc && dlc.Count <= 8 && dlc.All(value => value.Type == JTokenType.String),
                "Staged owner-seed record differs from its registered seed identity.");
            seedDlc = ((JArray)entry["dlc"]).Select(value => (string)value).ToArray();
            Require(seedDlc.Distinct(StringComparer.Ordinal).Count() == seedDlc.Length, "Owner-seed registered DLC list repeats.");
            VerifySourceBytes();
        }
        private void RequireAuthority(bool cleanupOnly)
        {
            Require(ReferenceEquals(Game.Instance, game) && ReferenceEquals(game.SaveManager, manager) &&
                authorization.IsActive && authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && authority.Root == root && manager.SavePath == root &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(root), "Areas") &&
                authority.MatchesAssistedColdInventoryPlan(request, game, manager) &&
                authority.AdmittedNativeLoadRequests <= 1 && authority.AdmittedNativeWriteRequests == 0 &&
                authorization.AuthorizedLoadCount <= 1 && authorization.AuthorizedWriteCount == 0 &&
                !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Cold inventory lost its exact one-load zero-write authority.");
            if (!cleanupOnly) Require(authorization.UnauthorizedLoadCount == 0 && authorization.UnauthorizedWriteCount == 0 &&
                authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 && authorization.BaselineLoadRequestCount == 0,
                "Cold inventory encountered a refused native boundary.");
            if (admitted)
            {
                if (cleanupOnly && (departure != null || selection.IsSixthHorse)) RequireWorldReferences();
                else RequireWorld();
            }
        }
        private void RequireWorldReferences()
        {
            Require(ReferenceEquals(nativePlayer, game.Player) && ReferenceEquals(state, game.State) &&
                ReferenceEquals(areaState, game.State?.LoadedAreaState) && ReferenceEquals(mainState, game.State?.LoadedAreaState?.MainState) &&
                ReferenceEquals(area, game.CurrentlyLoadedArea) && ReferenceEquals(mainActor, game.Player?.MainCharacter.Value) &&
                game.Player.GameId == request.PersistenceLoad.GameId && game.Player.MainCharacter.Value.CharacterName == request.PersistenceLoad.GameName &&
                game.Player.MainCharacter.Value.UniqueId == expectedMainId, "Admitted cold native world or main actor was replaced.");
        }
        private void RequireWorld()
        {
            RequireWorldReferences();
            if (departure != null) { RequireDepartureWorld(); return; }
            var current = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(game.Player.PartyCharacters.Select(value => value.Value).SequenceEqual(characters) &&
                game.Player.RemoteCompanions.Select(value => value.Value).SequenceEqual(remote), "Admitted raw native character/remote membership changed.");
            RequireLoader();
            Require(JToken.DeepEquals(loadedSemantics, CaptureSemantics()), "Admitted assisted cold progression or reciprocal field semantics changed.");
            if (selection.IsHorseWorld) Require(JToken.DeepEquals(thirdHorseFactsAtLoad, CaptureThirdHorseFacts()),
                "Admitted cold sourced Horse facts or ranks changed.");
            if (selection.IsSixthHorse) Require(JToken.DeepEquals(sixthRemoteAtLoad, CaptureSixthRemote()),
                "Admitted cold remote Linzi object, descriptor or progression changed.");
            Require(current.Length == actors.Length, "Admitted cold party count changed.");
            for (var i = 0; i < actors.Length; i++)
                Require(current.Count(actor => ReferenceEquals(actor, actors[i])) == 1 && ReferenceEquals(actors[i].Descriptor, descriptors[i]) &&
                    ReferenceEquals(actors[i].View, views[i]) && actors[i].IsInState && actors[i].IsInGame,
                    "Admitted cold actor, descriptor or view was replaced.");
        }
        private bool Busy() => LoadingProcess.Instance != null && (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any()) ||
            NativePersistenceIsolation.HasPendingWrites || persistence.AreaTransitionPending || persistence.HasActiveSaveScope ||
            persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private void RequireSelectorAuthority()
        {
            RequireAuthority(false); RequireNoOwners(); RequireNoEffects(); VerifySourceBytes();
            Require(selection.IsFourthHorseSelector && admitted && !Busy() && !game.Player.IsInCombat &&
                authority.AdmittedNativeWriteRequests == 0 && authorization.AuthorizedWriteCount == 0,
                "Opened selector lost exact fourth-Horse zero-write custody.");
        }
        private void RequireNoOwners() => Require(world.Registry.Owners.Length == 0 && !controls.HasUnsettledRelationshipTransition &&
            !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted &&
                pair.Relationship.PairOwner == null && pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasActiveCommand &&
                !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership), "Cold inventory found mounted custody.", Chunk7OwnerSeedAssertions.MountedCustody);
        private void RequireNoEffects() => Require(persistence.SnapshotCount == 0 && persistence.CompletedSaveCount == 0 &&
            persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0 &&
            controls.CapturePersistentSlots().Length == 0, "Read-only cold inventory observed native writing, cast or actor/pair restoration.", Chunk7OwnerSeedAssertions.NativeEffects);
        private JObject AuthoritySnapshot() => new JObject { ["root"] = root, ["generation"] = generation,
            ["maximumNativeLoads"] = authority.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests,
            ["writableEntries"] = authority.AssistedColdWritableEntries, ["entryCount"] = authority.AssistedColdEntryCount,
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
            ["nativeLoading"] = Busy(), ["authority"] = AuthoritySnapshot(), ["loadedDescriptor"] = probe.Identity(loader.Descriptor),
            ["party"] = new JArray((selection.IsFifthHorseDeparture || selection.IsSixthHorse ? Chunk7NativeFieldRoster.Capture(game.Player) : actors).Select(actor => new JObject { ["id"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                ["object"] = probe.Identity(actor), ["descriptor"] = probe.Identity(actor.Descriptor), ["view"] = probe.Identity(actor.View),
                ["isInGame"] = actor.IsInGame, ["isInState"] = actor.IsInState, ["commandsEmpty"] = actor.Commands.Empty,
                ["queuedCommands"] = actor.Commands.Queue.Count, ["conscious"] = actor.Descriptor.State.IsConscious, ["dead"] = actor.Descriptor.State.IsDead,
                ["inCombat"] = actor.IsInCombat, ["petId"] = actor.Descriptor.Pet?.UniqueId, ["masterId"] = actor.Descriptor.Master.Value?.UniqueId,
                ["characterLevel"] = actor.Descriptor.Progression.CharacterLevel, ["experience"] = actor.Descriptor.Progression.Experience, ["classes"] = CaptureClasses(actor) })),
            ["partyCharacterIds"] = new JArray((selection.IsFifthHorseDeparture || selection.IsSixthHorse ? game.Player.PartyCharacters.Select(value => value.Value) : characters).Select(value => value.UniqueId)),
            ["remoteCompanionIds"] = new JArray((selection.IsFifthHorseDeparture || selection.IsSixthHorse ? game.Player.RemoteCompanions.Select(value => value.Value) : remote).Select(value => value.UniqueId)), ["nativeMoney"] = game.Player.Money, ["dialogScheduled"] = game.Player.Dialog?.Scheduled != null,
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
                if (departure != null)
                {
                    if (!departure.TryDrain()) { Retain("Owned native departure selector has not closed."); return; }
                    evidence["nativeDeparture"] = departure.Capture();
                    if (errors.Count == 0) Require(departure.Completed && evidence["departureRetention"] != null,
                        "Native departure cannot PASS before its exact committed roster and retained Horse proof.");
                }
                if (optionality != null)
                {
                    if (!optionality.TryDrain()) { Retain("Owned native selector has not closed."); return; }
                    evidence["selectorProbe"] = optionality.Capture();
                }
                if (Busy() || probe?.InSynchronousCallback == true || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused)
                { Retain("Cold native load/callback or worker retains observer custody."); return; }
                RequireNoOwners();
                // Observation failures never authorize native cleanup mutations.
                // Once native custody is settled, release our own observers even
                // when a source/effect invariant failed; preserve the failure.
                try { RequireNoEffects(); } catch (Exception error) { Fail("C7-assisted-cold-cleanup", error.ToString()); }
                var sourceUnchanged = sourcePinned;
                try { VerifySourceBytes(); } catch (Exception error) { sourceUnchanged = false; Fail("C7-assisted-cold-cleanup", error.ToString()); }
                if (selection.IsSixthHorse && admitted && evidence["remoteSettled"] == null)
                {
                    try { RequireWorld(); evidence["remoteSettled"] = CaptureSixthRemote(); }
                    catch (Exception error) { Fail("C7-assisted-cold-cleanup", error.ToString()); }
                }
                if (admitted && evidence["settledWorld"] == null) {
                    evidence["settledWorld"] = Snapshot(); evidence["settledLoader"] = CaptureLoader();
                }
                if (probe != null && evidence["nativeLoad"] == null) evidence["nativeLoad"] = probe.Capture();
                if (restoration != null && evidence["noPreparation"] == null) evidence["noPreparation"] = restoration.Capture();
                // Both exact observers are attempted independently. A failed
                // unpatch keeps its field and prevents completion/host release.
                Exception failure = null;
                try { restoration?.Dispose(); } catch (Exception error) { failure = error; Fail("C7-assisted-cold-cleanup", error.ToString()); }
                try { resolution?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-assisted-cold-cleanup", error.ToString()); }
                try { probe?.Dispose(); } catch (Exception error) { failure = failure ?? error; Fail("C7-assisted-cold-cleanup", error.ToString()); }
                if (failure != null) throw new InvalidOperationException("Cold observer cleanup remains retained.", failure);
                RecordOwnerSeedPostQualification();
                Require(probe == null || probe.IdentityReleased, "Cold observer native labels remain retained.");
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["nativeWorkerSettled"] = !Busy(),
                    ["observersReleased"] = true, ["worldAdmitted"] = admitted, ["sourcePinned"] = sourcePinned, ["sourceUnchanged"] = sourceUnchanged,
                    ["authority"] = AuthoritySnapshot() };
                settled = true; Pass("C7-assisted-cold-cleanup");
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors);
                completed = true;
            }
            catch (Exception error) { Fail("C7-assisted-cold-cleanup", error.ToString()); Retain(error.Message); }
        }
        private void Retain(string reason)
        {
            if (pending || clock.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-assisted-cold-cleanup", reason + " The deadline does not relinquish custody.");
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", new JObject {
                ["cleanupPendingAtHostBoundary"] = true, ["cleanupCustodyRetained"] = true, ["reason"] = reason, ["authority"] = AuthoritySnapshot() }, errors);
            pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-assisted-cold-interrupted", "Cold inventory ended before its selected terminal proof."); BeginCleanup(); }
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
        // The harness's own assertion is marked so its stack (KMC frames by construction)
        // is never taken as evidence that KMC code faulted.
        private const string HarnessAssertionKey = "KingmakerMountedCombat.HarnessAssertion";
        private static void Require(bool value, string reason) => Require(value, reason, Chunk7OwnerSeedAssertions.Unidentified);
        // The assertion identifier is stable data (Chunk7OwnerSeedAssertions), never message text.
        private static void Require(bool value, string reason, string assertionId)
        {
            if (value) return;
            var error = new InvalidOperationException(reason); error.Data[HarnessAssertionKey] = true; error.Data[Chunk7OwnerSeedAssertions.AssertionIdKey] = assertionId; throw error;
        }
        private static bool IsHarnessAssertion(Exception error) => error.Data.Contains(HarnessAssertionKey);
    }
}
