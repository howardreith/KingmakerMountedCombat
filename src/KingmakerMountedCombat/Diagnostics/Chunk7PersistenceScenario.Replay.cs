using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        internal const string ThreeLoadsCase = "multi-exploration-three-loads-manual";
        internal const string ThreeAreasCase = "multi-exploration-three-area-transfers-manual";
        private string replaySelectedCase;
        private Chunk7OwnedPairsSelection replayOwnedSelection;
        private bool RepeatedWorldCase => replaySelectedCase != null || request.PersistenceCase == ThreeLoadsCase || request.PersistenceCase == ThreeAreasCase;
        private bool RepeatedAreaCase => (replaySelectedCase ?? request.PersistenceCase) == ThreeAreasCase;
        private readonly RuntimeSaveAuthorization replayAuthorization;
        private readonly PersistenceSaveAuthorization replaySaveAuthority;
        private int replayAuthorityGeneration;
        private Chunk7WorldRetentionProbe retention;
        private RetainedObjectIdentity replayIdentities;
        private readonly JArray replayWorlds = new JArray(), replayRequests = new JArray();
        private JObject replayArchive, replayLastWorld;
        private SaveInfo replayInput;
        private bool replayBegun, replayPending, replayCallback;
        private int replayCompletedLoads, replayCompletedAreas, replayOrdinal, replayReadyFrames;
        private int replayPublishedLoadOrdinal;
        private bool replayCleanupOnlyAdoption;
        private object replayPriorPlayer, replayPriorState, replayPriorAreaState, replayPriorMainState, replaySaveManager;
        private UnitEntityData[] replayPriorActors;
        private Chunk7OwnedPairFixture replayIncomingInherited;
        private readonly JArray replayInheritedRetirements = new JArray();
        private BlueprintArea replayDestination;
        private string[] replaySelectionIds;
        private string replaySubscriptionShape;
        private string ReplayCurrentArea => RepeatedAreaCase && replayCompletedAreas > 0 ?
            request.PersistenceReplayTargets[replayCompletedAreas - 1].Area : request.PersistenceLoad.Area;

        private void InitializeReplayAuthority()
        {
            if (!RepeatedWorldCase) return;
            Require(Cold && replaySelectedCase == null && replayAuthorization != null && replaySaveAuthority != null, "Repeated worlds require an explicit cold isolated authority.");
            replaySelectedCase = request.PersistenceCase; replayOwnedSelection = request.OwnedPairs;
            replayAuthorityGeneration = replayAuthorization.AuthorityGeneration;
            replaySaveManager = Game.Instance?.SaveManager;
            replayIdentities = new RetainedObjectIdentity(256);
            RequireReplayAuthority();
        }
        private void InstallReplayObservation()
        {
            if (!RepeatedWorldCase) return;
            RequireReplayAuthority();
            retention = new Chunk7WorldRetentionProbe(persistence, world, controls, replayIdentities);
            retention.Install();
        }
        private void RequireReplayAuthority(bool cleanupOnly = false)
        {
            Require(request.PersistenceCase == replaySelectedCase && ReferenceEquals(request.OwnedPairs, replayOwnedSelection) &&
                ReferenceEquals(beforeLoadGame, Game.Instance) && ReferenceEquals(replaySaveManager, Game.Instance?.SaveManager) &&
                NativePersistenceIsolation.IsExactAuthority(replaySaveAuthority) &&
                replayAuthorization != null && replayAuthorization.IsExactPersistenceScope(replaySaveAuthority) &&
                replayAuthorization.AuthorityGeneration == replayAuthorityGeneration &&
                Game.Instance?.SaveManager?.SavePath == replaySaveAuthority.Root &&
                replaySaveAuthority.MaximumNativeLoadRequests == (RepeatedAreaCase ? 1 : 3) &&
                replayAuthorization.AuthorizedWriteCount == 0 && (cleanupOnly || replayAuthorization.UnauthorizedLoadCount == 0 &&
                replayAuthorization.UnauthorizedWriteCount == 0 && replayAuthorization.FatalViolationCount == 0),
                "Repeated-world native save authority/root/generation changed or a write was attempted.");
            if (request.OwnedPairs != null)
                Require(request.OwnedPairs.Cold && Chunk7OwnedPairsSelection.RepeatedCheckpoint(request.PersistenceCase) &&
                    replaySaveAuthority.MatchesOwnedPairsPlan(true, false),
                    "Owned replay lost its closed immutable source/request/quota plan.");
        }
        private void BeginRepeatedWorld()
        {
            Require(!replayBegun && Cold && RepeatedWorldCase, "Repeated world sequence may begin once after the selected cold load.");
            RequireReplayAuthority(); RequireNativeWorld();
            replayInput = Game.Instance.SaveManager.Single(s => s.FileName == request.PersistenceLoad.FileName);
            RequireReplayInput();
            replaySelectionIds = originalSelection.Select(a => a.UniqueId).ToArray();
            BindRepeatedLoadedWorld(1, false);
            replayBegun = true;
            evidence["repeatedWorlds"] = new JObject { ["contract"] = "chunk7-repeated-worlds-v1", ["kind"] = RepeatedAreaCase ? "area-transfer" : "native-load",
                ["requiredNativeLoads"] = RepeatedAreaCase ? 1 : 3, ["requiredAreaTransfers"] = RepeatedAreaCase ? 3 : 0,
                ["worlds"] = replayWorlds, ["requests"] = replayRequests, ["performanceProof"] = false, ["diagnosticsOffProof"] = false,
                ["allocationRateProof"] = false, ["inputSha256"] = request.PersistenceLoad.Sha256 };
            Next(70);
        }
        private void RequireReplayInput()
        {
            var expected = request.PersistenceLoad;
            var path = Path.Combine(replaySaveAuthority.Root, expected.FileName);
            Require(replayInput != null && replayInput.Type == SaveInfo.SaveType.Manual && replayInput.FileName == expected.FileName &&
                replayInput.Name == expected.InternalName && replayInput.GameId == expected.GameId && replayInput.GameName == expected.GameName &&
                replayInput.Area?.AssetGuidThreadSafe == expected.Area && replayInput.CompatibilityVersion == 1 &&
                replayInput.OperationState == SaveInfo.StateType.None && replayInput.HasFileOnDisk &&
                Path.GetFullPath(replayInput.FolderName) == Path.GetFullPath(path) && new FileInfo(path).Length == expected.Length &&
                new FileInfo(path).LastWriteTimeUtc.Ticks == expected.LastWriteTimeUtcTicks && Hash(path) == expected.Sha256,
                "Repeated load input changed or escaped its exact immutable isolated descriptor.");
        }
        private bool ReplayNativeBoundaryIdle() => !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any() &&
            !persistence.LoadInFlight && !persistence.AreaTransitionPending && !persistence.CombatRestorationPending &&
            !world.SerializationBusy && !NativePersistenceIsolation.HasPendingWrites && !persistence.HasActiveSaveScope &&
            !persistence.HasSaveAdmissionFence && !controls.SerializationSuspended && !controls.HasUnsettledRelationshipTransition &&
            !world.UnifiedTurn.HasUnsettledPreparation && world.Pairs.All(p => !p.Combat.HasActiveCommand &&
                !p.Combat.HasActiveGroundMovement && !p.Combat.HasChargeOwnership && !controls.HasUnsettledShellFor(p.Relationship));

        // True while the selected repeated-world sequence owns Update. The main
        // loop must call this before its ordinary old-world scope check.
        private bool UpdateRepeatedWorld()
        {
            if (!RepeatedWorldCase || !replayBegun || stage != 70) return false;
            Require(!replayCleanupOnlyAdoption, "A cleanup-only native world cannot resume measured replay.");
            RequireReplayAuthority();
            if (!ReplayNativeBoundaryIdle()) { replayReadyFrames = 0; return true; }
            if (replayPending)
            {
                if (RepeatedAreaCase) AdmitRepeatedArea();
                else
                {
                    Require(replayCallback, "Native load pipeline ended without its exact per-dispatch callback.");
                    BindRepeatedLoadedWorld(replayOrdinal, true);
                }
                replayPending = false; replayReadyFrames = 0;
            }
            RequireNativeWorld(); RequireReplayInput(); AssertOwners(); AssertSlots(); AssertReplayQuiet();
            inherited?.RequirePreserved();
            if (++replayReadyFrames < 10) return true;
            if (RepeatedAreaCase ? replayCompletedAreas == 3 : replayCompletedLoads == 3)
            {
                AssertReplayObservers();
                var row = (JObject)evidence["repeatedWorlds"];
                row["nativeLoadRequests"] = replayAuthorization.AuthorizedLoadCount; row["nativeWriteRequests"] = replayAuthorization.AuthorizedWriteCount;
                row["restoration"] = restoration.Capture(); row["retention"] = retention.Capture(); row["inputUnchanged"] = true;
                snapshots.Add(Capture("repeated-worlds-complete"));
                Pass(RepeatedAreaCase ? "C7-persistence-three-native-area-transfers" : "C7-persistence-three-native-loads");
                BeginCleanup(); return true;
            }
            if (RepeatedAreaCase) DispatchRepeatedArea(); else DispatchRepeatedLoad();
            return true;
        }
        private void AssertReplayQuiet()
        {
            Require(Game.Instance.CurrentMode == GameModeType.Default && !Game.Instance.Player.IsInCombat && actors.Length == 4 &&
                actors.All(a => a.IsInState && a.IsInGame && a.View != null && a.Commands != null && a.Commands.Empty) &&
                world.BoundaryCount == 2 && world.FailedConstructionCount == 0 && world.UnifiedTurn.SnapshotParticipants.Length == 2 &&
                controls.NativeCastRequestCount == 0 && controls.CaptureSnapshot().DuplicateFactCount == 0 && persistence.SnapshotCount == 0 &&
                persistence.NativeLoadFailureCount == 0 && persistence.FailedAreaPairCount == 0,
                "Repeated world is not the exact settled exploration pair graph.");
        }
        internal static JObject CaptureRepeatedLoadCounts(int ordinal, bool replacement, int semantic, int presentation,
            int postLoads, int disposals, int authorized, int admitted, int frame, long gameTicks)
        {
            Require(ordinal >= 1 && ordinal <= 3, "Repeated load count journal has an undeclared epoch.");
            return new JObject { ["contract"] = "chunk7-repeated-load-counts-v1", ["ordinal"] = ordinal,
                ["replacement"] = replacement, ["frame"] = frame, ["gameTicks"] = gameTicks,
                ["actual"] = new JObject { ["semanticRestores"] = semantic, ["presentationRestores"] = presentation,
                    ["nativePostLoads"] = postLoads, ["nativeDisposals"] = disposals, ["authorizedLoads"] = authorized, ["admittedLoads"] = admitted },
                ["expected"] = new JObject { ["semanticRestores"] = ordinal * 4, ["presentationRestores"] = ordinal * 2,
                    ["nativePostLoads"] = 4, ["nativeDisposals"] = ordinal - 1, ["authorizedLoads"] = ordinal, ["admittedLoads"] = ordinal } };
        }
        internal static void ValidateRepeatedLoadCleanupEpoch(bool cleaning, bool failed, bool areaCase,
            int ordinal, int completed, int published, int pendingOrdinal, bool pending, bool callback, int authorized, int admitted)
        {
            Require(cleaning && failed && !areaCase && ordinal >= 2 && ordinal <= 3 &&
                (ordinal == completed + 1 || ordinal == completed && published == ordinal) &&
                ordinal == pendingOrdinal && pending && callback && authorized == ordinal && admitted == ordinal,
                "Cleanup-only replacement requires its exact failed callback-owned bounded load epoch.");
        }
        private void BindRepeatedLoadedWorld(int ordinal, bool replacement, bool cleanupOnly = false)
        {
            if (cleanupOnly) ValidateRepeatedLoadCleanupEpoch(cleanup, errors.Count != 0, RepeatedAreaCase, ordinal,
                replayCompletedLoads, replayPublishedLoadOrdinal, replayOrdinal, replayPending, replayCallback,
                replayAuthorization.AuthorizedLoadCount, replaySaveAuthority.AdmittedNativeLoadRequests);
            else Require(!replayCleanupOnlyAdoption, "Cleanup-only custody cannot qualify a measured load.");
            RequireReplayAuthority(cleanupOnly); RequireReplayInput();
            Require(ReplayNativeBoundaryIdle() && ordinal >= 1 && ordinal <= (RepeatedAreaCase ? 1 : 3), "Repeated loaded graph is not at an admissible native boundary.");
            var data = persistence.LoadedArchive;
            Require(data != null && data.SchemaVersion == 3 && data.Pairs.Length == 2 && data.Participations.Length == 0 && data.Combat == null &&
                data.CampaignId == request.PersistenceLoad.GameId && data.AreaId == request.PersistenceLoad.Area &&
                Game.Instance.Player.GameId == data.CampaignId && Game.Instance.Player.MainCharacter.Value.CharacterName == request.PersistenceLoad.GameName &&
                Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe == data.AreaId && persistence.ArchiveRestorePhase == "all-pair-presentation-restored",
                "Repeated load has not restored the exact complete native exploration archive.");
            var json = JObject.FromObject(data, MountedSaveCodec.CreateSerializer());
            Require(replayArchive == null || JToken.DeepEquals(replayArchive, json), "Repeated native load changed the selected semantic archive.");
            UnitEntityData[] next;
            if (request.OwnedPairs != null)
            {
                next = request.OwnedPairs.PairIds.Select(Exact).ToArray();
                Require(data.Pairs.All(pair => Enumerable.Range(0, 2).Count(index =>
                    pair.Rider.Id == next[index * 2].UniqueId && pair.Mount.Id == next[index * 2 + 1].UniqueId &&
                    pair.ProfileId == SupportedMountedProfiles.Resolve(next[index * 2], next[index * 2 + 1])?.Id) == 1) &&
                    data.Pairs.Select(pair => pair.Rider.Id).Distinct(StringComparer.Ordinal).Count() == 2,
                    "Repeated owned archive changed an exact acquired rider/pet role or live production profile.");
            }
            else
            {
                var mammoth = data.Pairs.Single(p => p.ProfileId == SupportedMountedProfiles.Mammoth.Id);
                var horse = data.Pairs.Single(p => p.ProfileId == SupportedMountedProfiles.Horse.Id);
                Require(mammoth.Rider.Id == Chunk7WorldScenarioEngine.MammothRiderId && mammoth.Mount.Id == Chunk7WorldScenarioEngine.MammothId &&
                    horse.Rider.Id == Chunk7WorldScenarioEngine.HorseRiderId, "Repeated input substituted a fixture actor.");
                next = new[] { Exact(mammoth.Rider.Id), Exact(mammoth.Mount.Id), Exact(horse.Rider.Id), Exact(horse.Mount.Id) };
            }
            Require(ReferenceEquals(next[0].Descriptor.Pet, next[1]) && ReferenceEquals(next[1].Descriptor.Master.Value, next[0]) &&
                ReferenceEquals(next[2].Descriptor.Pet, next[3]) && ReferenceEquals(next[3].Descriptor.Master.Value, next[2]) &&
                world.Registry.Owners.Length == 2 && world.Registry.ActiveCount == 2 && data.Pairs.All(p => {
                    var owner = world.Registry.FindActive(Exact(p.Rider.Id)); return owner != null && owner.Id.ToString("N") == p.PairId &&
                        owner.Generation == p.Generation && ReferenceEquals(owner.Mount, Exact(p.Mount.Id)); }),
                "Repeated load lost exact actor roles or imported stable owner generations.");
            var counts = CaptureRepeatedLoadCounts(ordinal, replacement, persistence.SemanticRestoreCount,
                persistence.PresentationRestoreCount, persistence.ArchivePostLoadObservedCount, persistence.NativeWorldDisposalCount,
                replayAuthorization.AuthorizedLoadCount, replaySaveAuthority.AdmittedNativeLoadRequests,
                UnityEngine.Time.frameCount, Game.Instance.TimeController.GameTime.Ticks);
            if (!cleanupOnly)
            {
                var journal = evidence["repeatedLoadCountChecks"] as JArray;
                if (journal == null) { journal = new JArray(); evidence["repeatedLoadCountChecks"] = journal; }
                Require(journal.Count < 3, "Repeated native count journal exceeded its three declared loads.");
                journal.Add(counts);
                // Freeze the observers before the assertion can divert into teardown.
                // These read-only snapshots grant no replacement-world admission.
                evidence["coldRestoration"] = restoration?.Capture();
                evidence["repeatedLoadRetentionAtBoundary"] = retention?.Capture();
                Require(persistence.SemanticRestoreCount == ordinal * 4 && persistence.PresentationRestoreCount == ordinal * 2 &&
                persistence.ArchivePostLoadObservedCount == 4 && persistence.NativeWorldDisposalCount == ordinal - 1 &&
                replayAuthorization.AuthorizedLoadCount == ordinal && replaySaveAuthority.AdmittedNativeLoadRequests == ordinal,
                "Native load/restoration count escaped its exact bounded epoch.");
            }
            if (replacement)
                Require(!ReferenceEquals(replayPriorPlayer, Game.Instance.Player) && ReferenceEquals(replayPriorState, Game.Instance.State) &&
                    !ReferenceEquals(replayPriorAreaState, Game.Instance.State.LoadedAreaState) &&
                    !ReferenceEquals(replayPriorMainState, Game.Instance.State.LoadedAreaState?.MainState) &&
                    next.All(a => replayPriorActors.All(old => !ReferenceEquals(a, old))), "Load callback did not replace the exact prior native world and actors.");
            AssertDebt(data);
            var actualSlots = controls.CapturePersistentSlots();
            Require(actualSlots.Length == data.Slots.Length && data.Slots.Length == 4 && data.Slots.All(s => actualSlots.Count(a => a.ActorId == s.ActorId && a.Index == s.Index && a.Kind == s.Kind) == 1),
                "Repeated world did not restore all native control slots exactly.");
            var selection = replaySelectionIds.Select(Exact).ToArray();
            if (request.OwnedPairs != null) AdmitOwnedRepeatedLoad(ordinal, replacement, next, selection, cleanupOnly);
            var admitted = inherited?.Scope ?? new Chunk7NativeWorldScope(request, next.Concat(selection), data.AreaId);
            // Commit a fresh scope only after native authority, callback, semantic
            // graph, debt and all owner/actor identities have passed together.
            nativeScope = admitted; actors = next; firstRider = next[0]; firstMount = next[1]; secondRider = next[2]; secondMount = next[3];
            originalSelection = selection; saved = data; slots = data.Slots;
            expectedOwners.Clear(); foreach (var pair in data.Pairs) expectedOwners.Add(Guid.Parse(pair.PairId), pair.Generation);
            replayArchive = replayArchive ?? json;
            if (cleanupOnly)
            {
                // The failed epoch is owned solely to release exact current
                // actors/leases. Never append a world or advance measured progress.
                replayCleanupOnlyAdoption = true;
                evidence["repeatedLoadCleanupAdoption"] = new JObject { ["contract"] = "chunk7-repeated-load-cleanup-only-v1",
                    ["ordinal"] = ordinal, ["acceptedLoadsBefore"] = replayCompletedLoads, ["counts"] = counts,
                    ["publishedLoadOrdinalBefore"] = replayPublishedLoadOrdinal,
                    ["exactCallbackObserved"] = true, ["nativeWorld"] = ReplaySnapshot(), ["qualifiesMeasuredLoad"] = false };
                return;
            }
            CompleteRepeatedLoadObservation(ordinal);
        }
        private void CompleteRepeatedLoadObservation(int ordinal)
        {
            retention.BindActors(actors);
            // Publishing native custody precedes fallible observer assertions.
            // A later failure must retain this exact pending epoch for cleanup,
            // without qualifying it or requesting another native load.
            replayPublishedLoadOrdinal = ordinal;
            replayCompletedLoads = ordinal;
            AssertReplayQuiet(); RecordRepeatedWorld("load-" + ordinal); AssertReplayObservers();
            evidence["loadedArchive"] = replayArchive.DeepClone();
            var descriptor = request.PersistenceLoad;
            evidence["loadedDescriptor"] = new JObject { ["internalName"] = descriptor.InternalName, ["fileName"] = descriptor.FileName,
                ["sha256"] = descriptor.Sha256, ["length"] = descriptor.Length, ["lastWriteTimeUtcTicks"] = descriptor.LastWriteTimeUtcTicks,
                ["gameId"] = descriptor.GameId, ["gameName"] = descriptor.GameName, ["area"] = descriptor.Area };
        }
        private void AdmitOwnedRepeatedLoad(int ordinal, bool replacement, UnitEntityData[] next, UnitEntityData[] selection, bool cleanupOnly = false)
        {
            Require(request.OwnedPairs != null && request.OwnedPairs.Cold && RepeatedWorldCase &&
                ordinal == (replacement ? replayOrdinal : 1) && (!replacement || replayPending && replayCallback),
                "Inherited replacement lacks its exact selected native callback epoch.");
            Require(!cleanupOnly || cleanup && errors.Count != 0 && replacement && inherited != null,
                "Inherited cleanup-only replacement cannot substitute for measured admission.");
            if (inherited == null)
            {
                Require(!replacement && ordinal == 1 && replayIncomingInherited == null, "A later native epoch cannot bypass initial inherited admission.");
                BindOwnedFixture(false);
            }
            else if (inherited.ReplayLoadOrdinal != ordinal)
            {
                Require(replacement && !RepeatedAreaCase, "Only a selected native load replaces inherited native objects.");
                if (replayIncomingInherited == null) replayIncomingInherited = cleanupOnly ?
                    inherited.CreateRepeatedLoadCleanupCandidate(ordinal, replayCallback) : inherited.CreateRepeatedLoadCandidate(ordinal, replayCallback);
                Require(cleanupOnly || !replayIncomingInherited.ReplayCleanupOnly,
                    "Cleanup-only inherited custody cannot qualify a measured successor.");
                Require(replayIncomingInherited.ReplayLoadOrdinal == ordinal, "A pending inherited candidate belongs to another load.");
                if (!replayIncomingInherited.Bound) replayIncomingInherited.Bind(false);
                replayIncomingInherited.RequirePreserved();
                // Capture every fresh field actor and fact in the common retained
                // registry before the old local identity lease may release.
                replayIncomingInherited.CaptureReplayField(replayIdentities.Get, "replay-field");
                var retirement = inherited.RetireAfterRepeatedLoad(replayIncomingInherited, replayIdentities.Get);
                replayInheritedRetirements.Add(retirement);
                inherited = replayIncomingInherited; replayIncomingInherited = null;
            }
            inherited.RequirePreserved();
            Require(cleanupOnly || !inherited.ReplayCleanupOnly, "Cleanup-only inherited custody cannot resume measured replay.");
            Require(inherited.Actors.Length == next.Length && inherited.Actors.Where((actor, index) => !ReferenceEquals(actor, next[index])).Count() == 0 &&
                selection.All(actor => inherited.Party.Any(field => ReferenceEquals(field, actor))),
                "Native replay actor roles or original selection escaped the complete inherited field roster.");
        }
        private void DispatchRepeatedLoad()
        {
            RequireNativeWorld(); RequireReplayAuthority(); RequireReplayInput(); AssertReplayQuiet();
            Require(!replayCleanupOnlyAdoption && replayCompletedLoads >= 1 && replayCompletedLoads < 3 && !replayPending,
                "Native repeated load ordinal is already owned or exhausted.");
            var decision = replaySaveAuthority.Validate(RuntimeSaveOperation.Load, ReplayTarget(), replaySaveAuthority.Root);
            Require(decision == null, "Next native repeated load was refused before world replacement: " + decision);
            var before = ReplaySnapshot();
            inherited?.BeginRepeatedLoad(replayCompletedLoads + 1);
            replayPriorPlayer = Game.Instance.Player; replayPriorState = Game.Instance.State;
            replayPriorAreaState = Game.Instance.State.LoadedAreaState;
            replayPriorMainState = Game.Instance.State.LoadedAreaState?.MainState;
            replayPriorActors = actors.ToArray();
            replayOrdinal = replayCompletedLoads + 1; replayCallback = false; replayPending = true; replayReadyFrames = 0;
            var token = replayOrdinal;
            replayRequests.Add(new JObject { ["kind"] = "native-load", ["ordinal"] = token, ["before"] = before,
                ["fileName"] = replayInput.FileName, ["sha256"] = request.PersistenceLoad.Sha256, ["callbackObserved"] = false });
            Game.Instance.SaveManager.AddCallbackAfterLoad(() => {
                if (!replayPending || replayOrdinal != token || replayCallback || !ReferenceEquals(replaySaveManager, Game.Instance?.SaveManager) ||
                    replayAuthorization.AuthorizedLoadCount != token || replaySaveAuthority.AdmittedNativeLoadRequests != token) { Fail("C7-persistence-runtime", "Duplicate or stale repeated native load callback."); BeginCleanup(); return; }
                replayCallback = true;
                var receipt = (JObject)replayRequests[replayRequests.Count - 1];
                receipt["callbackObserved"] = true; receipt["callbackNativeLoads"] = replayAuthorization.AuthorizedLoadCount;
                receipt["callbackFrame"] = UnityEngine.Time.frameCount;
            });
            // The installed Game.LoadGame routes through LoadArea without first
            // disposing the prior Player/area state. This replay requires a
            // replacement epoch; the native main-menu entry disposes the area
            // and Player before it delegates to LoadGame. PersistentState itself
            // remains the exact same Game.State object.
            Game.Instance.LoadGameFromMainMenu(replayInput);
        }
        private RuntimeSaveTarget ReplayTarget() => new RuntimeSaveTarget { FileName = replayInput.FileName, InternalName = replayInput.Name,
            GameId = replayInput.GameId, GameName = replayInput.GameName, Area = replayInput.Area.AssetGuidThreadSafe,
            SaveType = replayInput.Type.ToString(), FullPath = replayInput.FolderName };
        private void DispatchRepeatedArea()
        {
            RequireNativeWorld(); RequireReplayAuthority(); RequireReplayInput(); AssertReplayQuiet();
            Require(replayCompletedAreas < 3 && !replayPending, "Native repeated area ordinal is already owned or exhausted.");
            var target = request.PersistenceReplayTargets[replayCompletedAreas];
            var enter = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(target.EnterPoint);
            Require(target.AutoSaveMode == "None" && enter != null && enter.AssetGuidThreadSafe == target.EnterPoint && enter.Area != null &&
                enter.Area.AssetGuidThreadSafe == target.Area && Game.Instance.CurrentlyLoadedArea != enter.Area &&
                Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe != target.Area, "Declared area route is unresolved or would be a same-area no-op.");
            var before = ReplaySnapshot();
            inherited?.BeginRepeatedArea(replayCompletedAreas + 1, enter.Area);
            replayDestination = enter.Area; replayOrdinal = replayCompletedAreas + 1; replayPending = true; replayReadyFrames = 0;
            replayRequests.Add(new JObject { ["kind"] = "area-transfer", ["ordinal"] = replayOrdinal,
                ["enterPoint"] = target.EnterPoint, ["area"] = target.Area, ["autoSaveMode"] = "None", ["before"] = before });
            Game.Instance.LoadArea(enter, AutoSaveMode.None);
        }
        private void AdmitRepeatedArea()
        {
            RequireReplayAuthority();
            var target = request.PersistenceReplayTargets[replayOrdinal - 1];
            Require(replayPending && replayOrdinal == replayCompletedAreas + 1 && replayDestination != null &&
                persistence.AreaSuspensionCount == replayOrdinal && persistence.AreaResumeCount == replayOrdinal * 2 &&
                persistence.NativeWorldDisposalCount == 0 && persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 &&
                replayAuthorization.AuthorizedLoadCount == 1 && replaySaveAuthority.AdmittedNativeLoadRequests == 1,
                "Area transition cannot adopt before exact native suspension/all-owner restoration.");
            nativeScope.RequireDeclaredAreaDestination(target.Area, replayDestination);
            AssertOwners(); AssertSlots(); AssertDebt(saved); AssertReplayQuiet();
            foreach (var actor in actors)
            {
                var prior = ((JArray)replayLastWorld["actors"]).Single(a => (string)a["id"] == actor.UniqueId);
                Require((int)prior["view"] == actor.View.GetInstanceID(), "Ordinary area transfer replaced an original native view.");
            }
            foreach (var rider in new[] { firstRider, secondRider })
            {
                var relation = world.Registry.FindActive(rider).Boundary;
                var current = relation.Runtime.MovementAgent;
                var previous = ((JArray)replayLastWorld["actors"]).Single(a => (string)a["id"] == rider.UniqueId);
                Require(current != null && current.GetInstanceID() != (int)previous["agentOverride"] &&
                    rider.View.GetComponents<RiderMovementAgent>().Length == 1 && ReferenceEquals(rider.View.AgentOverride, current) &&
                    relation.Runtime.ValidateMountedInvariants() == null, "Area transfer reused or duplicated prior movement authority.");
            }
            inherited?.AdoptRepeatedArea(replayOrdinal, replayDestination);
            nativeScope = inherited?.Scope ?? new Chunk7NativeWorldScope(request, actors.Concat(originalSelection), target.Area);
            replayCompletedAreas = replayOrdinal; RecordRepeatedWorld("area-" + replayOrdinal); AssertReplayObservers();
        }
        private JObject ReplaySnapshot()
        {
            var value = Chunk7WorldRetentionProbe.CaptureWorld(world, controls, persistence, actors, replayIdentities);
            if (request.OwnedPairs != null)
            {
                Require(inherited != null && inherited.Bound && replayIncomingInherited == null, "Owned replay has no fully published native field epoch.");
                value["ownedField"] = inherited.CaptureReplayField(replayIdentities.Get, "replay-field");
            }
            return value;
        }
        private void RecordRepeatedWorld(string label)
        {
            var state = ReplaySnapshot(); state["label"] = label;
            state["selectionRebinding"] = new JArray(originalSelection.Select(a => new JObject { ["id"] = a.UniqueId,
                ["object"] = replayIdentities.Get(a) }));
            var subscriptions = state["subscriptions"];
            Require((bool)subscriptions["executing"] == false, "Settled subscriber inventory overlaps native dispatch.");
            var entries = (JArray)subscriptions["entries"];
            var horse = "KingmakerMountedCombat.Integration.HorseCompanionAddPet";
            var lifecycle = "KingmakerMountedCombat.Integration.MountedLifecycleSubscriber";
            var controlsType = "KingmakerMountedCombat.Integration.NativeMountedControlService";
            var shape = string.Join(";", entries.Select(e => (string)e["interface"] + "|" + (string)e["type"])
                .OrderBy(s => s, StringComparer.Ordinal));
            var horses = entries.Where(e => (string)e["type"] == horse).Select(e => (int)e["identity"]).ToArray();
            var lifecycleIds = entries.Where(e => (string)e["type"] == lifecycle).Select(e => (int)e["identity"]).Distinct().ToArray();
            var controlIds = entries.Where(e => (string)e["type"] == controlsType).Select(e => (int)e["identity"]).Distinct().ToArray();
            // One native HorseCompanionAddPet subscriber per KMC Horse owner: count pairs whose
            // profile mounts the KMC Horse blueprint, Small Halfling Horse included (b194 x4c).
            var expectedHorses = ((JArray)replayArchive["Pairs"]).Count(p =>
                SupportedMountedProfiles.ResolveProfileId((string)p["ProfileId"])?.BlueprintGuid == SupportedMountedProfiles.HorseBlueprintGuid);
            var persistent = entries.Where(e => (string)e["type"] != horse)
                .Select(e => (string)e["interface"] + "|" + (string)e["type"] + "|" + (int)e["identity"])
                .OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Require(expectedHorses >= 1 && expectedHorses <= 2 &&
                (int)subscriptions["uniqueOwnedSubscribers"] == 2 + expectedHorses &&
                (int)subscriptions["ownedInterfaceEntries"] == 15 + expectedHorses &&
                entries.Count == 15 + expectedHorses && entries.Count(e => (string)e["type"] == lifecycle) == 13 &&
                entries.Count(e => (string)e["type"] == controlsType) == 2 && horses.Length == expectedHorses &&
                horses.Distinct().Count() == expectedHorses && lifecycleIds.Length == 1 && controlIds.Length == 1 &&
                lifecycleIds[0] != controlIds[0] && horses.All(id => id != lifecycleIds[0] && id != controlIds[0]) &&
                entries.Where(e => (string)e["type"] == horse).All(e =>
                    (string)e["interface"] == "Kingmaker.PubSubSystem.ISceneHandler") && entries.All(e =>
                    (string)e["type"] == horse || (string)e["type"] == lifecycle || (string)e["type"] == controlsType) &&
                (replaySubscriptionShape == null || replaySubscriptionShape == shape),
                "Repeated world accumulated or lost native KMC subscriber roles: expected Horse owners " + expectedHorses +
                ", unique " + (int)subscriptions["uniqueOwnedSubscribers"] + ", entries " + entries.Count +
                ", Horse subscribers " + horses.Length + ", lifecycle " + entries.Count(e => (string)e["type"] == lifecycle) +
                ", controls " + entries.Count(e => (string)e["type"] == controlsType) + ".");
            if (replayLastWorld != null)
            {
                var previous = (JArray)replayLastWorld["subscriptions"]["entries"];
                var previousPersistent = previous.Where(e => (string)e["type"] != horse)
                    .Select(e => (string)e["interface"] + "|" + (string)e["type"] + "|" + (int)e["identity"])
                    .OrderBy(s => s, StringComparer.Ordinal).ToArray();
                var previousHorses = previous.Where(e => (string)e["type"] == horse).Select(e => (int)e["identity"]).ToArray();
                Require(persistent.SequenceEqual(previousPersistent) && previousHorses.Length == expectedHorses &&
                    (RepeatedAreaCase ? horses.OrderBy(id => id).SequenceEqual(previousHorses.OrderBy(id => id)) :
                        horses.All(id => !previousHorses.Contains(id))),
                    "Repeated world retained stale fact subscribers or changed process-owned subscribers.");
            }
            replaySubscriptionShape = replaySubscriptionShape ?? shape; replayLastWorld = state; replayWorlds.Add(state);
            if (request.OwnedPairs != null) evidence["inheritedReplayRetirements"] = replayInheritedRetirements;
        }
        private void AssertReplayObservers()
        {
            Require(restoration != null && restoration.Complete && retention != null && retention.Complete, "Repeated world observer is incomplete.");
            var events = (JArray)restoration.Capture()["events"];
            Require(!events.Any(e => (string)e["kind"] == "prepare-before" || (string)e["kind"] == "round-before" ||
                (string)e["kind"] == "restore-actor-before" || (string)e["kind"] == "shared-world-restored") &&
                events.Count(e => (string)e["kind"] == "restore-debt-before") == replayCompletedLoads * 4 &&
                events.Count(e => (string)e["kind"] == "restore-debt-after") == replayCompletedLoads * 4 &&
                events.Count(e => (string)e["kind"] == "all-owner-presentation-restored") == replayCompletedLoads,
                "Repeated native world restoration invented preparation/round/combat state or replayed debt.");
        }
        private void RequireRepeatedCleanupWorld()
        {
            if (!RepeatedWorldCase) return;
            RequireReplayAuthority(true);
            if (replayPending && ReplayNativeBoundaryIdle())
            {
                if (RepeatedAreaCase) AdmitRepeatedArea();
                else { Require(replayCallback, "Failed repeated native load retains unadmitted world cleanup custody."); BindRepeatedLoadedWorld(replayOrdinal, true, true); }
                replayPending = false;
            }
            Require(!replayPending, "Repeated world still owns an unfinished native boundary.");
        }
        private void CaptureRepeatedSettlement()
        {
            if (!RepeatedWorldCase) return;
            if (!replayBegun) { replayIdentities?.Dispose(); return; }
            Require(retention == null && restoration == null && mode == null && !replayPending, "Repeated-world observers or admission still own cleanup custody.");
            RequireNativeWorld(); RequireReplayAuthority(true); RequireReplayInput();
            if (replayIdentities.Released)
            {
                Require(evidence["repeatedWorlds"]?["settled"] != null && replayIdentities.FaultCount == 0 && replayIdentities.RetainedCount == 0, "Released replay identities lack a completed settlement receipt.");
                return;
            }
            var final = ReplaySnapshot();
            Require((int)final["active"] == 0 && ((JArray)final["owners"]).Count == 0 && (int)final["preparedOrViewResidue"] == 0 &&
                (int)final["liveControlBoundaries"] == 0 && (int)final["commandOwners"] == 0 && !(bool)final["unsettledPreparation"] &&
                !(bool)final["relationshipTransition"] && !(bool)final["serializationBusy"] && !(bool)final["pendingWrites"] &&
                !(bool)final["saveScope"] && !(bool)final["saveFence"], "Repeated-world cleanup retains native owners or command/save custody.");
            var row = (JObject)evidence["repeatedWorlds"]; row["settled"] = final; row["observersReleased"] = true;
            replayIdentities.Dispose();
            row["identityRetention"] = new JObject { ["capacity"] = replayIdentities.Capacity, ["faults"] = replayIdentities.FaultCount,
                ["retainedAfterRelease"] = replayIdentities.RetainedCount, ["released"] = replayIdentities.Released, ["ids"] = new JArray(replayIdentities.Ids) };
            Require(replayIdentities.FaultCount == 0 && replayIdentities.Released && replayIdentities.RetainedCount == 0, "Repeated native identity registry failed to release exactly.");
        }
        private void EndReplayObservation()
        {
            if (retention != null) ((JObject)evidence["repeatedWorlds"] ?? evidence)["finalRetention"] = retention.Capture();
            retention?.Dispose(); retention = null;
        }
    }
}
