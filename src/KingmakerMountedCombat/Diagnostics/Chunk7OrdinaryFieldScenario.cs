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
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Loads the paid acquisition archive unchanged, then uses the ordinary native
    // selector. Auto_1 was written while remote. This case grants no save, pose,
    // mount, progression, or replacement authority.
    internal sealed class Chunk7OrdinaryFieldScenario : IDisposable
    {
        internal const string EvidenceFileName = "chunk7-ordinary-field-evidence.json";
        internal const string EvidenceKind = "chunk7-ordinary-field-evidence";
        internal const string PendingFileName = "chunk7-ordinary-field-cleanup-pending.json";
        internal const string PendingKind = "chunk7-ordinary-field-cleanup-pending";
        private readonly RuntimeRequest request;
        private readonly Chunk7OrdinaryFieldSelection selection;
        private readonly RuntimeSaveAuthorization authorization;
        private readonly PersistenceSaveAuthorization authority;
        private readonly MountedWorldService world;
        private readonly MountedPersistenceService persistence;
        private readonly NativeMountedControlService controls;
        private readonly HorseCompanionBlueprintService horses;
        private readonly int generation;
        private readonly string root;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly JObject evidence = new JObject();
        private readonly List<string> errors = new List<string>();
        private readonly List<RuntimeSubscenarioResult> results = new List<RuntimeSubscenarioResult>();
        private Chunk7NativeWorldScope scope;
        private Chunk7NativePartyFieldAdmission field;
        private UnitEntityData actor, pet;
        private UnitEntityData[] originalActors, originalSelection;
        private string[] originalParty;
        private JObject sourceSelection, before;
        private IDisposable ummLease;
        private bool captured, pause, cleanup, settled, completed, disposed, pending;
        private double cleanupStart;
        private string sourcePath, archivePath;
        private long sourceLength, sourceTicks, archiveLength, archiveTicks, castsBefore;
        private int snapshotsBefore, savesBefore;
        internal bool IsCompleted => completed;
        internal IReadOnlyList<string> Errors => errors;
        internal IReadOnlyList<RuntimeSubscenarioResult> Results => results;
        internal static bool SupportsScenario(string scenario) => scenario == Chunk7OrdinaryFieldSelection.Scenario;
        internal Chunk7OrdinaryFieldScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
        {
            if (request?.OrdinaryField == null || request.OrdinaryField.Validate(request.Scenario, request.RunId).Count != 0 || request.PersistenceLoad == null)
                throw new ArgumentException("An exact read-only field-admission request is required.");
            this.request = request; selection = request.OrdinaryField;
            this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            generation = authorization.AuthorityGeneration;
            root = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)), "runtime-staging", "persistence-" + request.RunId, "Saved Games");
        }
        internal void Update()
        {
            if (completed || disposed) return;
            try
            {
                if (cleanup) { PollCleanup(); return; }
                if (!captured) Start();
                Require(clock.Elapsed.TotalSeconds <= 180, "Read-only field admission exceeded its bounded native readiness window.");
                AssertAuthority(); if (Busy()) return;
                AssertInput();
                if (!field.Completed && !field.Tick()) return;
                if (actor.View?.AgentASP == null || pet.View?.AgentASP == null || !actor.IsInGame || !pet.IsInGame) return;
                var body = NativeSmallRiderBody.Capture(actor, pet);
                if (body?.NativeRigAvailable != true) return;
                RequireBody(body);
                Require(actor.IsInState && pet.IsInState && actor.IsDirectlyControllable && pet.IsDirectlyControllable &&
                    Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                    Game.Instance.Player.PartyCharacters.Count == originalParty.Length + 1 &&
                    originalParty.All(id => Game.Instance.Player.PartyCharacters.Count(value => value.Value?.UniqueId == id) == 1) &&
                    !Game.Instance.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, actor)), "Native selector did not place the exact companion and retain its original party.");
                AssertUnchangedProgression();
                evidence["selector"] = field.Capture();
                evidence["field"] = Snapshot();
                var inventory = Chunk7NativeProfileInventory.Capture(new[] { actor, pet });
                evidence["profileInventory"] = inventory;
                Require((string)inventory["captureStatus"] == "PASS" && (int)inventory["schemaVersion"] == 1 &&
                    inventory["party"] is JArray inventoryActors && inventoryActors.Count == 2 &&
                    inventoryActors.Children<JObject>().Count(value => (string)value["id"] == actor.UniqueId &&
                        value["character"]?["present"]?.Type == JTokenType.Boolean && (bool)value["character"]["present"]) == 1,
                    "Native field actor placed, but its bounded exact rider rig inventory is unavailable.");
                evidence["bodyObservation"] = JObject.FromObject(body);
                Pass("C7-field-native-party-placement"); BeginCleanup();
            }
            catch (Exception error) { Fail("C7-field-runtime", error.ToString()); BeginCleanup(); }
        }
        private void Start()
        {
            AssertAuthority(); AssertInput();
            Require(Game.Instance.CurrentMode == GameModeType.Default && request.Fixture.Working.Sha256 == Chunk7WorldScenarioEngine.WorkingHash &&
                horses.HorseFeature != null && horses.HorseUnit != null && horses.LevelRank != null &&
                Game.Instance.UI.CharacterBuildController.LevelUpController == null && Game.Instance.UI.CharacterBuildController.Unit == null,
                "Exact idle native Working campaign and closed builder are required.");
            RequireNoOwners(); ReadSource();
            actor = Exact(selection.ActorId); pet = actor.Descriptor.Pet;
            Require(pet != null && pet.UniqueId == selection.PetId && pet.Blueprint == horses.HorseUnit && ReferenceEquals(pet.Descriptor.Master.Value, actor) &&
                actor.Blueprint == BlueprintRoot.Instance.CustomCompanion && actor.Descriptor.OriginalSize == Size.Small && actor.Descriptor.State.Size == Size.Small &&
                actor.GetActivePolymorph() == null && pet.GetActivePolymorph() == null &&
                Game.Instance.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                !Game.Instance.Player.PartyCharacters.Any(value => ReferenceEquals(value.Value, actor)) && !actor.IsInGame &&
                actor.Descriptor.GetFact(horses.HorseFeature) is Feature fact && fact.Source != null && actor.Descriptor.GetFact(horses.LevelRank)?.GetRank() > 0,
                "Cold archive did not restore the one exact remote Small custom companion and sourced Horse.");
            originalParty = Game.Instance.Player.PartyCharacters.Select(value => value.Value.UniqueId).ToArray();
            Require(originalParty.Length > 0 && originalParty.Length < 6 && originalParty.Distinct().Count() == originalParty.Length,
                "A distinct original party with one free native slot is required.");
            originalActors = new[] { Chunk7WorldScenarioEngine.MammothRiderId, Chunk7WorldScenarioEngine.MammothId, Chunk7WorldScenarioEngine.HorseRiderId }.Select(Exact).ToArray();
            Require(originalActors.All(value => value.IsInState && value.IsInGame && value.View?.AgentASP != null && value.Commands.Empty) &&
                originalParty.Contains(Chunk7WorldScenarioEngine.MammothRiderId) && originalParty.Contains(Chunk7WorldScenarioEngine.HorseRiderId), "Qualified source original actors changed or are busy.");
            scope = new Chunk7NativeWorldScope(request, originalActors.Concat(new[] { actor, pet }));
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray(); pause = Game.Instance.IsPaused;
            before = Snapshot(); snapshotsBefore = persistence.SnapshotCount; savesBefore = persistence.CompletedSaveCount; castsBefore = controls.NativeCastRequestCount;
            captured = true; ummLease = MovementScreenshotCaptureCoordinator.AcquireClosedUmmLease();
            evidence["processId"] = Process.GetCurrentProcess().Id; evidence["selection"] = Chunk7EvidenceDto.Request(selection);
            evidence["before"] = before; evidence["authority"] = AuthoritySnapshot();
            evidence["sourceWasRemoteAutosave"] = true; evidence["sourceArchiveStillRemote"] = true;
            evidence["smallPoseQualified"] = false; evidence["mountedCombatProven"] = false;
            evidence["freshMainCampaignProven"] = false; evidence["manualPartyMutation"] = false;
            Game.Instance.IsPaused = false;
            field = new Chunk7NativePartyFieldAdmission(actor, originalParty, AssertInput);
            field.Open(); Pass("C7-field-readonly-source");
        }
        private void ReadSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7OrdinaryFieldSelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == request.PersistenceLoad.Length && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256,
                "Exact staged source artifact/archive is unavailable or changed.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks;
            var sourceArtifact = JObject.Parse(File.ReadAllText(sourcePath)); var o = sourceArtifact["observations"];
            Require((string)sourceArtifact["runId"] == selection.SourceRunId && (string)sourceArtifact["scenario"] == Chunk7OrdinaryAcquisitionSelection.Scenario &&
                (string)sourceArtifact["evidenceKind"] == Chunk7OrdinaryAcquisitionScenario.EvidenceKind && (string)sourceArtifact["status"] == "PASS" &&
                sourceArtifact["errors"] is JArray sourceErrors && sourceErrors.Count == 0 && (string)sourceArtifact["commit"] == request.Commit &&
                (string)sourceArtifact["dllSha256"] == request.DllSha256 && (string)sourceArtifact["dllMvid"] == request.DllMvid &&
                (string)o?["case"] == Chunk7OrdinaryAcquisitionSelection.PaidSmallCase && (string)o?["field"]?["actorId"] == selection.ActorId &&
                (string)o?["field"]?["petId"] == selection.PetId && (string)o?["nativeWrite"]?["sha256"] == selection.SourceArchiveSha256 &&
                o?["nativeWrite"]?["saveBeforeFieldSelection"]?.Type == JTokenType.Boolean && (bool)o["nativeWrite"]["saveBeforeFieldSelection"],
                "The staged acquisition source lacks exact same-candidate remote-autosave provenance.");
            sourceSelection = Chunk7EvidenceDto.ReadDeployedOrdinarySelection((JObject)o["selection"], selection.SourceRunId);
            evidence["source"] = new JObject { ["runId"] = selection.SourceRunId, ["artifactSha256"] = selection.SourceArtifactSha256,
                ["archiveSha256"] = selection.SourceArchiveSha256, ["archiveLeaf"] = request.PersistenceLoad.FileName,
                ["archiveName"] = request.PersistenceLoad.InternalName, ["archiveLength"] = archiveLength,
                ["bodySelection"] = sourceSelection.DeepClone(), ["sameCandidate"] = true, ["unchangedArchive"] = true };
        }
        private void RequireBody(SmallRiderBodyObservation body)
        {
            Require(body.NativeRaceAndPresetIdentityExact && body.NativeSkeletonIsExactPreset && body.NativeRigAvailable && !body.RiderPolymorphed && !body.MountPolymorphed &&
                body.OriginalSizeOrdinal == (int)Size.Small && body.EffectiveSizeOrdinal == (int)Size.Small &&
                body.RaceGuid == (string)sourceSelection["smallRaceGuid"] && body.PresetGuid == (string)sourceSelection["smallPresetGuid"] &&
                body.SkeletonName == (string)sourceSelection["skeletonName"] && body.Gender == (string)sourceSelection["gender"] &&
                body.LeftHanded == (bool)sourceSelection["leftHanded"], "Field body differs from its explicit native acquisition source.");
        }
        // Existing deployed ordinary artifacts use JObject.FromObject without a
        // resolver: exactly the DTO's PascalCase properties. Preserve those raw
        // source bytes, validate that exact typed shape, then deliberately emit
        // the field request's camelCase shape for downstream evidence readers.
        private void AssertAuthority()
        {
            Require(authorization.IsActive && authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && authority.Root == root && Game.Instance?.SaveManager?.SavePath == root &&
                NativePersistenceIsolation.ObservedStashFolder == Path.Combine(Path.GetDirectoryName(root), "Areas") &&
                authority.MatchesReadOnlyFieldPlan(request.PersistenceLoad) && authority.AdmittedNativeLoadRequests == 1 && authority.AdmittedNativeWriteRequests == 0 &&
                authorization.AuthorizedLoadCount == 1 && authorization.AuthorizedWriteCount == 0 && authorization.UnauthorizedLoadCount == 0 &&
                authorization.UnauthorizedWriteCount == 0 && authorization.FatalViolationCount == 0 && authorization.SuppressedWorkingWriteCount == 0 &&
                authorization.BaselineLoadRequestCount == 0 && !authorization.IsOneShotWorkingWriteSuppressionArmed,
                "Exact read-only field authority, load count, root or generation changed.");
            Require(Game.Instance.Player != null && Game.Instance.Player.GameId == request.Fixture.Working.GameId &&
                Game.Instance.Player.MainCharacter.Value?.CharacterName == request.Fixture.Working.GameName &&
                Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe == request.PersistenceLoad.Area, "Field admission lost its exact campaign/area.");
            scope?.RequireCurrent();
            if (sourcePath != null)
            {
                var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
                Require(source.Exists && source.Length == sourceLength && source.LastWriteTimeUtc.Ticks == sourceTicks &&
                    archive.Exists && archive.Length == archiveLength && archive.LastWriteTimeUtc.Ticks == archiveTicks, "Read-only source bytes changed during field admission.");
            }
        }
        private void AssertInput()
        {
            AssertAuthority();
            Require(!Busy() && !Game.Instance.Player.IsInCombat && !Game.Instance.CutsceneLock && !Game.Instance.IsModeActive(GameModeType.Dialog) &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown), "Native busy state/combat/dialog prevents field UI input.");
        }
        private bool Busy() => LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() || NativePersistenceIsolation.HasPendingWrites ||
            persistence.AreaTransitionPending || persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence || world.SerializationBusy;
        private void RequireNoOwners() => Require(world.Registry.Owners.Length == 0 && !controls.HasUnsettledRelationshipTransition &&
            !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted &&
            pair.Relationship.PairOwner == null && pair.Relationship.Runtime.NoPreparedPairResidue), "Field-only admission has unexpected mounted/native preparation ownership.");
        private JObject Snapshot() => new JObject { ["actorId"] = actor.UniqueId, ["actorObject"] = Id(actor), ["descriptorObject"] = Id(actor.Descriptor),
            ["petId"] = pet.UniqueId, ["petObject"] = Id(pet), ["petDescriptorObject"] = Id(pet.Descriptor), ["money"] = Game.Instance.Player.Money,
            ["xp"] = actor.Descriptor.Progression.Experience, ["level"] = actor.Descriptor.Progression.CharacterLevel,
            ["petLevel"] = pet.Descriptor.Progression.CharacterLevel, ["rank"] = actor.Descriptor.GetFact(horses.LevelRank).GetRank(),
            ["featureSourceGuid"] = ((Feature)actor.Descriptor.GetFact(horses.HorseFeature)).Source.AssetGuid,
            ["riderInGame"] = actor.IsInGame, ["petInGame"] = pet.IsInGame,
            ["riderInState"] = actor.IsInState, ["petInState"] = pet.IsInState,
            ["riderAgentReady"] = actor.View?.AgentASP != null, ["petAgentReady"] = pet.View?.AgentASP != null,
            ["reciprocal"] = ReferenceEquals(actor.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, actor),
            ["partyCharacters"] = new JArray(Game.Instance.Player.PartyCharacters.Select(value => value.Value.UniqueId)),
            ["remoteCompanions"] = new JArray(Game.Instance.Player.RemoteCompanions.Select(value => value.Value.UniqueId)) };
        private void AssertUnchangedProgression()
        {
            var now = Snapshot();
            foreach (var key in new[] { "actorId", "actorObject", "descriptorObject", "petId", "petObject", "petDescriptorObject", "money", "xp", "level", "petLevel", "rank", "featureSourceGuid", "reciprocal" })
                Require(JToken.DeepEquals(before[key], now[key]), "Field selection changed native progression or reciprocal identity: " + key);
            Require(persistence.SnapshotCount == snapshotsBefore && persistence.CompletedSaveCount == savesBefore && controls.NativeCastRequestCount == castsBefore,
                "Field admission unexpectedly requested a mounted ability or wrote a native save.");
        }
        private JObject AuthoritySnapshot() => new JObject { ["root"] = root, ["generation"] = generation, ["exactScope"] = authorization.IsExactPersistenceScope(authority),
            ["maximumNativeLoads"] = authority.MaximumNativeLoadRequests, ["maximumNativeWrites"] = authority.MaximumNativeWriteRequests,
            ["admittedNativeLoads"] = authority.AdmittedNativeLoadRequests, ["admittedNativeWrites"] = authority.AdmittedNativeWriteRequests,
            ["authorizedLoads"] = authorization.AuthorizedLoadCount, ["authorizedWrites"] = authorization.AuthorizedWriteCount,
            ["unauthorizedLoads"] = authorization.UnauthorizedLoadCount, ["unauthorizedWrites"] = authorization.UnauthorizedWriteCount,
            ["fatalViolations"] = authorization.FatalViolationCount, ["suppressedWrites"] = authorization.SuppressedWorkingWriteCount };
        private UnitEntityData Exact(string id)
        {
            var values = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Where(value => value?.UniqueId == id).Distinct().ToArray();
            Require(values.Length == 1, "Exact source actor is missing or duplicated: " + id); return values[0];
        }
        private void BeginCleanup() { if (cleanup) return; cleanup = true; cleanupStart = clock.Elapsed.TotalSeconds; }
        private void PollCleanup()
        {
            try
            {
                if (completed) return;
                if (Busy() || persistence.DrainForTeardown(0) == OwnedWorkerTeardownVerdict.Refused) { Retain("Native worker/load retains field-admission custody."); return; }
                if (!captured)
                {
                    // Admission can fail before a UI lease or native mutation exists.
                    // Preserve FAIL without pretending that an invalid source passed,
                    // or creating cleanup inputs merely to settle an unused engine.
                    evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["neverAcquiredNativeUi"] = true };
                    settled = true;
                    RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, "FAIL", evidence, errors); completed = true; return;
                }
                AssertInput();
                if (field != null && !field.TryDrain()) { Retain("The owned native party selector has not closed."); return; }
                if (captured)
                {
                    RequireNoOwners(); AssertUnchangedProgression();
                    SelectionManager.Instance.MultiSelect(originalSelection.Where(value => value.IsInState && value.View != null).Select(value => value.View), false);
                    Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "Original native selection did not restore exactly.");
                    ummLease?.Dispose(); ummLease = null; Game.Instance.IsPaused = pause;
                    Require(Hash(sourcePath) == selection.SourceArtifactSha256 && Hash(archivePath) == selection.SourceArchiveSha256, "Read-only source changed before terminal evidence.");
                }
                evidence["cleanup"] = new JObject { ["custodySettled"] = true, ["nativeWorkerSettled"] = !Busy(), ["selectorClosed"] = field == null || !field.Owned,
                    ["selectionRestored"] = true, ["pauseRestored"] = !captured || Game.Instance.IsPaused == pause, ["acquiredNativeStateRetained"] = true,
                    ["sourceUnchanged"] = true, ["nativeWrites"] = 0, ["manualPartyMutations"] = 0, ["manualRefunds"] = 0,
                    ["manualFactRemovals"] = 0, ["manualCompanionRemovals"] = 0, ["authority"] = AuthoritySnapshot() };
                settled = true; Pass("C7-field-cleanup"); evidence["elapsedSeconds"] = clock.Elapsed.TotalSeconds;
                RuntimeLoaderInventory.WriteArtifact(request, EvidenceFileName, EvidenceKind, errors.Count == 0 ? "PASS" : "FAIL", evidence, errors); completed = true;

            }
            catch (Exception error)
            {
                Fail("C7-field-cleanup", error.ToString());
                Retain("Field-admission cleanup guard or release refused: " + error.Message);
            }
        }
        private void Retain(string reason)
        {
            if (pending || clock.Elapsed.TotalSeconds - cleanupStart <= 45) return;
            Fail("C7-field-cleanup", reason + " The failure deadline never relinquishes custody.");
            RuntimeLoaderInventory.WriteArtifact(request, PendingFileName, PendingKind, "FAIL", new JObject { ["reason"] = reason,
                ["cleanupCustodyRetained"] = true, ["cleanupPendingAtHostBoundary"] = true, ["authority"] = AuthoritySnapshot() }, errors); pending = true;
        }
        internal bool DrainForHost()
        {
            if (disposed) return true;
            if (!cleanup) { Fail("C7-field-interrupted", "Read-only field admission interrupted before completion."); BeginCleanup(); }
            if (!completed) PollCleanup(); return completed && settled;
        }
        public void Dispose() { if (disposed) return; if (!DrainForHost()) throw new InvalidOperationException("Native field-admission custody remains retained."); disposed = true; }
        private void Pass(string row) { if (results.All(value => value.Name != row)) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "PASS", AssertionPassCount = 1, Errors = Array.Empty<string>() }); }
        private void Fail(string row, string error)
        {
            if (errors.Contains(error)) return; errors.Add(error); var value = results.SingleOrDefault(result => result.Name == row);
            if (value == null) results.Add(new RuntimeSubscenarioResult { Name = row, Status = "FAIL", AssertionFailCount = 1, Errors = new[] { error } });
            else { value.Status = "FAIL"; value.AssertionFailCount++; value.Errors = value.Errors.Concat(new[] { error }).ToArray(); }
        }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant(); }
        private static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
    }
}
