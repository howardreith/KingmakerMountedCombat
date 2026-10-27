using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UI.SettingsUI;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private bool AreaAutosaveCase => request.PersistenceCase == "multi-area-autosave";
        private string ExpectedArchiveArea => request.ExpectedChunk7ArchiveArea;
        private Player areaWorld;
        private BlueprintArea areaDestination;
        private bool areaTransferDispatched, areaScopeAdopted;
        private bool areaObservedSuspension, areaObserverAttached;
        private int areaLoadingTicks;
        private long areaCastBaseline, areaSnapshotBaseline;
        private int areaSemanticBaseline, areaPresentationBaseline, areaPostLoadBaseline;
        private string areaObserverError;
        private JObject areaBefore, areaHeaderStarting, areaHeaderStaged;
        private readonly Dictionary<string, int> areaViewIds = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> areaMovementIds = new Dictionary<string, int>(StringComparer.Ordinal);
        private SavedNativeActor[] areaActorDebt;
        private long areaNativeTicks;
        private readonly JObject areaContinuationPlans = new JObject();

        // Invoked after both native Mount shells settle and owned slots are bound.
        // The declared native area entry is the sole source of the autosave.
        // This controlled navigation is not evidence of ordinary travel or an authored exit.
        private void BeginAreaAutosave()
        {
            RequireNativeWorld();
            var game = Game.Instance; var target = request.PersistenceAreaTarget;
            Require(AreaAutosaveCase && !Cold && target != null && target.AutoSaveMode == "AfterEntry" &&
                target.Area != request.SelectedChunk7Fixture.Area && game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.SelectedChunk7Fixture.Area,
                "Area autosave requires its exact declared destination and AfterEntry policy.");
            Require(!game.Player.IsInCombat && !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any() &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !persistence.AreaTransitionPending &&
                persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 && !NativePersistenceIsolation.HasPendingWrites,
                "Area autosave started across an unsettled native world boundary.");
            AssertOwners(); AssertSlots();
            Require(actors.Length == 4 && actors.All(a => a.Commands.Empty && a.View?.AgentASP != null && a.IsInState && a.IsInGame),
                "Area autosave lacks four exact idle actors and live native views.");
            BlueprintAreaEnterPoint enter;
            if (OwnerSeedCheckpointSave)
            {
                JObject prerequisite;
                enter = request.OwnedPairs.IsManticoreReturnArea ?
                    Chunk7ManticoreReturnEntry.Resolve(game, request, out prerequisite) :
                    Chunk7OwnerSeedAreaEntry.Resolve(game, request, out prerequisite);
                evidence["areaEntryPrerequisite"] = prerequisite;
                Require(!enter.Area.IsCapital,
                    "The selected native destination is currently a capital; both saved pairs cannot be directly controllable there.");
            }
            else enter = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(target.EnterPoint);
            Require(enter != null && enter.AssetGuidThreadSafe == target.EnterPoint && enter.Area != null &&
                enter.Area.AssetGuidThreadSafe == target.Area && enter.Area != game.CurrentlyLoadedArea,
                "Area autosave cannot resolve the exact authorized native enter point.");
            Require(SettingsRoot.Instance.AutosaveEnabled.CurrentValue && game.SaveManager.IsSaveAllowed() &&
                !game.SaveManager.Any(s => s.Type == SaveInfo.SaveType.Auto && s.IsActuallySaved),
                "Native AfterEntry autosave is disabled, refused or would rotate an existing slot.");
            if (request.OwnedPairs != null) RequireOwnedSaveAdmission(SaveInfo.SaveType.Auto);
            NativePersistenceIsolation.EnableNativeSlotRotation();
            areaWorld = game.Player; areaNativeTicks = game.TimeController.GameTime.Ticks;
            areaActorDebt = actors.Select(MountedPersistenceService.CaptureActor).ToArray();
            foreach (var actor in actors) areaViewIds.Add(actor.UniqueId, actor.View.GetInstanceID());
            foreach (var rider in new[] { firstRider, secondRider })
            {
                var relationship = world.Registry.FindActive(rider).Boundary;
                Require(relationship.Runtime.MovementAgent != null && relationship.Runtime.ValidateMountedInvariants() == null,
                    "Area baseline lacks exact mounted movement authority.");
                areaMovementIds.Add(rider.UniqueId, relationship.Runtime.MovementAgent.GetInstanceID());
            }
            areaCastBaseline = controls.NativeCastRequestCount; areaSnapshotBaseline = persistence.SnapshotCount;
            areaSemanticBaseline = persistence.SemanticRestoreCount; areaPresentationBaseline = persistence.PresentationRestoreCount;
            areaPostLoadBaseline = persistence.ArchivePostLoadObservedCount;
            Require(areaSemanticBaseline == (OwnerSeedCheckpointSave ? 4 : 0) && areaPresentationBaseline == (OwnerSeedCheckpointSave ? 2 : 0) &&
                areaPostLoadBaseline == (OwnerSeedCheckpointSave ? 4 : 0), "Area transition lacks its exact admitted native restoration baseline.");
            castBefore = areaCastBaseline; snapshotBefore = areaSnapshotBaseline; callback = false;
            if (inherited != null)
            {
                areaIdentities = new RetainedObjectIdentity(128);
                inherited.BeginAreaAutosave(enter.Area);
            }
            areaBefore = CaptureAreaWorld("before-native-transfer");
            evidence["areaTransfer"] = new JObject { ["mode"] = target.AutoSaveMode, ["enterPoint"] = target.EnterPoint,
                ["sourceArea"] = request.SelectedChunk7Fixture.Area, ["destinationArea"] = target.Area, ["before"] = areaBefore,
                ["nativeSaveCallbackExpected"] = false, ["explicitSaveGameDispatches"] = 0 };
            persistence.SaveSnapshotStarting += ObserveAreaAutosaveStarting;
            persistence.SaveSnapshotStaged += ObserveAreaAutosaveStaged;
            areaObserverAttached = true;
            areaDestination = enter.Area;
            Next(30);
            areaTransferDispatched = true;
            game.LoadArea(enter, AutoSaveMode.AfterEntry);
            ((JObject)evidence["areaTransfer"])["dispatchReturned"] = true;
        }

        private void CaptureAreaInterruption()
        {
            if (!AreaAutosaveCase || !(evidence["areaTransfer"] is JObject)) return;
            var row = (JObject)evidence["areaTransfer"];
            row["interruption"] = new JObject {
                ["frame"] = UnityEngine.Time.frameCount,
                ["dispatchReturned"] = row["dispatchReturned"]?.Value<bool>() == true,
                ["nativeLoading"] = LoadingProcess.Instance.IsLoadingInProcess,
                ["queuedLoadNames"] = LoadingProcess.Instance.QueuedNames.Count(),
                ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                ["destinationIsCapital"] = Game.Instance.CurrentlyLoadedArea?.IsCapital,
                ["samePlayer"] = ReferenceEquals(areaWorld, Game.Instance.Player),
                ["actorControl"] = new JArray((actors ?? new UnitEntityData[0]).Select(actor => new JObject {
                    ["id"] = actor?.UniqueId, ["inGame"] = actor?.IsInGame,
                    ["directlyControllable"] = actor?.IsDirectlyControllable,
                    ["masterId"] = actor == null || actor.Descriptor == null ? null : actor.Descriptor.Master.Value?.UniqueId
                })),
                ["suspensions"] = persistence.AreaSuspensionCount,
                ["resumes"] = persistence.AreaResumeCount,
                ["failedPairs"] = persistence.FailedAreaPairCount,
                ["transitionPending"] = persistence.AreaTransitionPending,
                ["activeOwners"] = world.Registry.ActiveCount,
                ["nativeWritePending"] = NativePersistenceIsolation.HasPendingWrites,
                ["saveScope"] = persistence.HasActiveSaveScope,
                ["saveFence"] = persistence.HasSaveAdmissionFence,
                ["feedback"] = persistence.Feedback
            };
        }

        // Called before the usual LoadingProcess early return so the asynchronous
        // suspension interval remains observable. Returns true while this stage owns Update.
        private bool UpdateAreaAutosave()
        {
            if (!AreaAutosaveCase || Cold || stage != 30) return false;
            if (LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any())
            {
                areaLoadingTicks++;
                if (persistence.AreaSuspensionCount == 1 && world.Registry.ActiveCount == 0)
                    areaObservedSuspension = true;
                // Retain only the latest native-loading boundary. A timeout
                // still reports whether the engine reached either owner resume.
                ((JObject)evidence["areaTransfer"])["awaitingNativeReady"] = new JObject {
                    ["frame"] = UnityEngine.Time.frameCount,
                    ["nativeLoading"] = LoadingProcess.Instance.IsLoadingInProcess,
                    ["queuedLoadNames"] = LoadingProcess.Instance.QueuedNames.Count(),
                    ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                    ["samePlayer"] = ReferenceEquals(areaWorld, Game.Instance.Player),
                    ["suspensions"] = persistence.AreaSuspensionCount,
                    ["resumes"] = persistence.AreaResumeCount,
                    ["failedPairs"] = persistence.FailedAreaPairCount,
                    ["transitionPending"] = persistence.AreaTransitionPending,
                    ["activeOwners"] = world.Registry.ActiveCount,
                    ["nativeWritePending"] = NativePersistenceIsolation.HasPendingWrites,
                    ["saveScope"] = persistence.HasActiveSaveScope,
                    ["saveFence"] = persistence.HasSaveAdmissionFence,
                    ["feedback"] = persistence.Feedback
                };
                return true;
            }
            if (persistence.AreaResumeCount < 2 || persistence.AreaTransitionPending || NativePersistenceIsolation.HasPendingWrites ||
                persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence)
            {
                // Keep one bounded, read-only observation of the native area
                // pipeline when it has stopped loading but cannot reach the
                // all-owner autosave barrier. This grants no destination scope.
                ((JObject)evidence["areaTransfer"])["awaitingNativeReady"] = new JObject {
                    ["frame"] = UnityEngine.Time.frameCount,
                    ["area"] = Game.Instance.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                    ["samePlayer"] = ReferenceEquals(areaWorld, Game.Instance.Player),
                    ["suspensions"] = persistence.AreaSuspensionCount,
                    ["resumes"] = persistence.AreaResumeCount,
                    ["failedPairs"] = persistence.FailedAreaPairCount,
                    ["transitionPending"] = persistence.AreaTransitionPending,
                    ["activeOwners"] = world.Registry.ActiveCount,
                    ["nativeWritePending"] = NativePersistenceIsolation.HasPendingWrites,
                    ["saveScope"] = persistence.HasActiveSaveScope,
                    ["saveFence"] = persistence.HasSaveAdmissionFence,
                    ["feedback"] = persistence.Feedback
                };
                return true;
            }
            AdoptDeclaredAreaWorld();
            var archive = Game.Instance.SaveManager.SingleOrDefault(s => s.Type == SaveInfo.SaveType.Auto && s.Name == NativeName(SaveInfo.SaveType.Auto));
            if (archive == null || !archive.HasFileOnDisk || archive.OperationState != SaveInfo.StateType.None) return true;
            var after = CaptureAreaWorld("after-native-transfer");
            var row = (JObject)evidence["areaTransfer"]; row["after"] = after;
            row["headerStarting"] = areaHeaderStarting; row["headerStaged"] = areaHeaderStaged;
            row["observerError"] = areaObserverError; row["loadingTicks"] = areaLoadingTicks; row["suspensionObserved"] = areaObservedSuspension;
            Require(areaObserverError == null && areaHeaderStarting != null && areaHeaderStaged != null && areaLoadingTicks > 0 && areaObservedSuspension,
                "Native area/autosave observation missed a required causal boundary.");
            Require(ReferenceEquals(areaWorld, Game.Instance.Player) && Game.Instance.CurrentMode == GameModeType.Default &&
                Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe == ExpectedArchiveArea && persistence.AreaSuspensionCount == 1 &&
                persistence.AreaResumeCount == 2 && persistence.FailedAreaPairCount == 0 && persistence.FailedSaveCount == 0 &&
                persistence.SemanticRestoreCount == areaSemanticBaseline && persistence.PresentationRestoreCount == areaPresentationBaseline && persistence.ArchivePostLoadObservedCount == areaPostLoadBaseline && controls.NativeCastRequestCount == areaCastBaseline &&
                persistence.SnapshotCount == areaSnapshotBaseline + 1,
                "Native area transition replayed native input/debt, omitted an owner or saved at another boundary.");
            AssertOwners(); AssertSlots(); AssertAreaRestoredViews(); AssertAreaNativeDebt();
            Require((string)areaHeaderStarting["area"] == ExpectedArchiveArea && (int)areaHeaderStarting["suspensions"] == 1 &&
                (int)areaHeaderStarting["resumes"] == 2 && (int)areaHeaderStarting["activeOwners"] == 2 &&
                (long)areaHeaderStarting["snapshots"] == areaSnapshotBaseline &&
                (string)areaHeaderStaged["area"] == ExpectedArchiveArea && (int)areaHeaderStaged["resumes"] == 2 &&
                (int)areaHeaderStaged["activeOwners"] == 2 && (long)areaHeaderStaged["snapshots"] == areaSnapshotBaseline + 1,
                "The native autosave header ran before both attachments were restored.");
            // No callback is invented: normal ObserveSaved must admit this exact
            // engine-autosave completion separately from an explicit SaveGame callback.
            savedDescriptor = archive;
            EndAreaObservation(); ObserveSaved();
            var sourceHash = Hash(archive.FolderName);
            persistence.RestoreAreaPair(); persistence.RestoreAreaPair();
            Require(persistence.AreaResumeCount == 2 && Hash(archive.FolderName) == sourceHash &&
                controls.NativeCastRequestCount == areaCastBaseline, "Duplicate area readiness replayed restoration or rewrote the autosave.");
            row["duplicateReadyNoOp"] = true; row["nativeArchiveSha256"] = sourceHash;
            Pass("C7-persistence-all-pair-area-before-autosave"); Next(10); return true;
        }

        private void ObserveAreaAutosaveStarting()
        {
            try
            {
                AdoptDeclaredAreaWorld();
                if (areaHeaderStarting != null) throw new InvalidOperationException("Area case observed a second native header start.");
                areaHeaderStarting = CaptureAreaWorld("native-header-starting");
                if (inherited != null) areaHeaderStarting["nativeSaveBoundary"] = ObserveOwnedAreaHeader(false);
                AssertOwners(); AssertAreaRestoredViews();
            }
            catch (Exception error) { areaObserverError = areaObserverError ?? error.ToString(); }
        }
        private void ObserveAreaAutosaveStaged()
        {
            try
            {
                AdoptDeclaredAreaWorld();
                if (areaHeaderStaged != null) throw new InvalidOperationException("Area case observed a second native header stage.");
                areaHeaderStaged = CaptureAreaWorld("native-header-staged");
                if (inherited != null) areaHeaderStaged["nativeSaveBoundary"] = ObserveOwnedAreaHeader(true);
                AssertOwners(); AssertAreaRestoredViews(true);
            }
            catch (Exception error) { areaObserverError = areaObserverError ?? error.ToString(); }
        }
        private void AdoptDeclaredAreaWorld()
        {
            if (areaScopeAdopted) { RequireNativeWorld(); return; }
            var target = request.PersistenceAreaTarget;
            Require(AreaAutosaveCase && !Cold && stage == 30 && areaTransferDispatched && target != null &&
                target.AutoSaveMode == "AfterEntry" && areaDestination != null &&
                areaDestination.AssetGuidThreadSafe == target.Area && target.Area != request.SelectedChunk7Fixture.Area &&
                ReferenceEquals(areaWorld, Game.Instance?.Player) && persistence.AreaSuspensionCount == 1 &&
                persistence.AreaResumeCount == 2 && persistence.FailedAreaPairCount == 0 &&
                persistence.FailedSaveCount == 0 && persistence.SemanticRestoreCount == areaSemanticBaseline && persistence.PresentationRestoreCount == areaPresentationBaseline && persistence.ArchivePostLoadObservedCount == areaPostLoadBaseline &&
                controls.NativeCastRequestCount == areaCastBaseline,
                "Native destination cannot be adopted before the exact declared all-owner area restoration.");
            Require(nativeScope != null, "Area destination has no captured source world authority.");
            nativeScope.RequireDeclaredAreaDestination(target.Area, areaDestination);
            AssertOwners(); AssertSlots(); AssertAreaRestoredViews();
            // The prior source scope is immutable. Only this explicit completed
            // native transfer establishes the new loaded-area/main-state pins.
            inherited?.AdoptAreaAutosave(areaDestination);
            nativeScope = inherited?.Scope ?? new Chunk7NativeWorldScope(request, actors.Concat(originalSelection), target.Area);
            areaScopeAdopted = true;
        }

        private JObject CaptureAreaWorld(string label)
        {
            var game = Game.Instance;
            var row = Capture(label);
            row["area"] = game?.CurrentlyLoadedArea?.AssetGuidThreadSafe;
            row["sameWorld"] = ReferenceEquals(areaWorld, game?.Player);
            row["suspensions"] = persistence.AreaSuspensionCount; row["resumes"] = persistence.AreaResumeCount;
            row["pending"] = persistence.AreaTransitionPending; row["activeOwners"] = world.Registry.ActiveCount;
            row["activeSaveScope"] = persistence.HasActiveSaveScope;
            row["views"] = new JArray(actors.Select(actor => new JObject { ["id"] = actor.UniqueId,
                ["viewId"] = actor.View == null ? 0 : actor.View.GetInstanceID(), ["baselineViewId"] = areaViewIds[actor.UniqueId],
                ["viewBound"] = actor.View != null && ReferenceEquals(actor.View.EntityData, actor),
                ["nativeMatches"] = game.State.Units.Concat(game.Player.AllCharacters).Where(a => a?.UniqueId == actor.UniqueId).Distinct().Count(),
                ["commandsEmpty"] = actor.Commands.Empty }));
            row["movementAuthorities"] = new JArray(new[] { firstRider, secondRider }.Select(rider => {
                var relationship = world.Registry.FindActive(rider)?.Boundary; var runtime = relationship?.Runtime;
                return new JObject { ["riderId"] = rider.UniqueId, ["baselineOverride"] = areaMovementIds[rider.UniqueId],
                    ["runtimeOverride"] = runtime?.MovementAgent == null ? 0 : runtime.MovementAgent.GetInstanceID(),
                    ["viewOverride"] = rider.View?.AgentOverride == null ? 0 : rider.View.AgentOverride.GetInstanceID(),
                    ["overrideComponents"] = new JArray(rider.View == null ? new int[0] : rider.View.GetComponents<RiderMovementAgent>().Select(a => a.GetInstanceID()).ToArray()),
                    ["invariant"] = label == "native-header-staged" ? runtime?.ValidateMountedSaveSerializationInvariants() : runtime?.ValidateMountedInvariants(),
                    ["saveSerializationSuspended"] = relationship?.SaveSerializationSuspended,
                    ["stockEnabled"] = rider.View?.AgentASP?.enabled,
                    ["mountId"] = relationship?.Mount?.UniqueId, ["mountStockEnabled"] = relationship?.Mount?.View?.AgentASP?.enabled,
                    ["mountOverride"] = relationship?.Mount?.View?.AgentOverride == null ? 0 : relationship.Mount.View.AgentOverride.GetInstanceID(),
                    ["mountOverrideComponents"] = new JArray(relationship?.Mount?.View == null ? new int[0] : relationship.Mount.View.GetComponents<RiderMovementAgent>().Select(a => a.GetInstanceID()).ToArray()),
                    ["riderViewBoundToOwner"] = relationship?.IsExactCapturedView(rider),
                    ["mountViewBoundToOwner"] = relationship?.IsExactCapturedView(relationship.Mount) };
            }));
            if (inherited != null)
            {
                row["ownedField"] = inherited.CaptureReplayField(areaIdentities.Get, "replay-field");
                row["ownedWorld"] = new JObject { ["game"] = areaIdentities.Get(game), ["player"] = areaIdentities.Get(game.Player),
                    ["manager"] = areaIdentities.Get(game.SaveManager), ["worldScope"] = areaIdentities.Get(nativeScope),
                    ["state"] = areaIdentities.Get(game.State), ["areaState"] = areaIdentities.Get(game.State.LoadedAreaState),
                    ["mainState"] = areaIdentities.Get(game.State.LoadedAreaState.MainState),
                    ["authorityGeneration"] = replayAuthorization.AuthorityGeneration };
            }
            return row;
        }
        private void AssertAreaRestoredViews(bool stagedSerialization = false)
        {
            if (stagedSerialization)
                Require(persistence.HasActiveSaveScope && persistence.SaveSuspended,
                    "Area staged header has no exact active native serialization scope.");
            foreach (var actor in actors)
                Require(ReferenceEquals(Exact(actor.UniqueId), actor) && actor.View != null && ReferenceEquals(actor.View.EntityData, actor) &&
                    actor.View.GetInstanceID() == areaViewIds[actor.UniqueId], "Ordinary area transfer did not retain its exact native actor/view.");
            foreach (var rider in new[] { firstRider, secondRider })
            {
                var relation = world.Registry.FindActive(rider)?.Boundary; var runtime = relation?.Runtime;
                var overrides = rider.View.GetComponents<RiderMovementAgent>();
                Require(runtime?.MovementAgent != null && runtime.MovementAgent.GetInstanceID() != areaMovementIds[rider.UniqueId] &&
                    overrides.Length == 1 && ReferenceEquals(overrides[0], runtime.MovementAgent) && ReferenceEquals(rider.View.AgentOverride, runtime.MovementAgent) &&
                    !rider.View.AgentASP.enabled && relation.Mount.View.AgentASP.enabled && relation.Mount.View.AgentOverride == null &&
                    relation.Mount.View.GetComponents<RiderMovementAgent>().Length == 0 && relation.IsExactCapturedView(rider) &&
                    relation.IsExactCapturedView(relation.Mount) &&
                    (stagedSerialization ? relation.SaveSerializationSuspended &&
                        runtime.ValidateMountedSaveSerializationInvariants() == null :
                        !relation.SaveSerializationSuspended && runtime.ValidateMountedInvariants() == null),
                    "Area restoration duplicated or reused stale movement/view authority.");
            }
        }
        private void AssertAreaNativeDebt()
        {
            var elapsed = Math.Max(0, (Game.Instance.TimeController.GameTime.Ticks - areaNativeTicks) / (double)TimeSpan.TicksPerSecond);
            foreach (var before in areaActorDebt)
            {
                var after = MountedPersistenceService.CaptureActor(Exact(before.Id));
                var a = new[] { before.Standard, before.Move, before.Swift, before.Initiative, before.Reaction };
                var b = new[] { after.Standard, after.Move, after.Swift, after.Initiative, after.Reaction };
                Require(before.ReactionsRemaining == after.ReactionsRemaining && before.LastSurpriseTicks == after.LastSurpriseTicks &&
                    Enumerable.Range(0, a.Length).All(i => b[i] <= a[i] + 0.001 && b[i] + elapsed + 0.001 + elapsed * 0.1 >= a[i]),
                    "Area transfer lost native actor expenditure outside observed clock progress.");
            }
        }
        // Spider Cave's inherited mount position is beside native geometry. A
        // fixed world-axis click can terminate against that geometry even when
        // the restored movement authority is sound. Select the first of eight
        // exact two-metre rays whose stock navmesh trace reaches a free endpoint.
        // This is read-only fixture planning; Move still sends one ordinary click
        // and MovementSettled keeps the same travel/residual requirements.
        private UnityEngine.Vector3 SelectAreaContinuationGoal(UnitEntityData mover, string label)
        {
            Require(AreaAutosaveCase && mover?.View?.AgentASP != null && AstarPath.active != null,
                "Area continuation has no exact stock movement/navmesh boundary.");
            var start = mover.Position;
            Require(FiniteAreaPoint(start) && FiniteAreaNumber(mover.View.Corpulence) && mover.View.Corpulence > 0f,
                "Area continuation has no finite native mover position or footprint.");
            var attachedRider = ReferenceEquals(mover, firstMount) ? firstRider : secondRider;
            var proposals = new JArray();
            foreach (var angle in new[] { 90f, 270f, 0f, 180f, 45f, 135f, 225f, 315f })
            {
                var requested = start + UnityEngine.Quaternion.Euler(0f, angle, 0f) * UnityEngine.Vector3.forward * 2f;
                var nearest = AstarPath.active.GetNearest(requested);
                var walkable = nearest.node != null && nearest.node.Walkable;
                var goal = nearest.clampedPosition;
                if (!FiniteAreaPoint(goal)) { proposals.Add(new JObject { ["angle"] = angle, ["requested"] = Point(requested), ["eligible"] = false }); continue; }
                var traceEnd = walkable ? Kingmaker.View.ObstacleAnalyzer.TraceAlongNavmesh(start, goal) : start;
                if (!FiniteAreaPoint(traceEnd)) { proposals.Add(new JObject { ["angle"] = angle, ["requested"] = Point(requested), ["goal"] = Point(goal), ["eligible"] = false }); continue; }
                var horizontalTravel = HorizontalDistance(start, goal);
                var clampDistance = HorizontalDistance(requested, goal);
                var routeResidual = HorizontalDistance(traceEnd, goal);
                var occupied = Game.Instance.State.Units.Concat(Game.Instance.Player.AllCharacters).Distinct().Any(actor =>
                    actor != null && !ReferenceEquals(actor, mover) && !ReferenceEquals(actor, attachedRider) &&
                    actor.IsInGame && actor.View != null && (!FiniteAreaPoint(actor.Position) ||
                    !FiniteAreaNumber(actor.View.Corpulence) || HorizontalDistance(actor.Position, goal) <
                    mover.View.Corpulence + actor.View.Corpulence + 0.05f));
                var eligible = walkable && clampDistance <= 0.15f &&
                    horizontalTravel >= 1.8f && horizontalTravel <= 2.2f && routeResidual <= 0.05f && !occupied;
                proposals.Add(new JObject { ["angle"] = angle, ["requested"] = Point(requested), ["goal"] = Point(goal),
                    ["traceEnd"] = Point(traceEnd), ["walkable"] = walkable,
                    ["travel"] = horizontalTravel, ["clampDistance"] = clampDistance,
                    ["routeResidual"] = routeResidual, ["occupied"] = occupied, ["eligible"] = eligible });
                if (!eligible) continue;
                areaContinuationPlans[label] = new JObject {
                    ["moverId"] = mover.UniqueId, ["start"] = Point(start), ["selectedAngle"] = angle,
                    ["selectedGoal"] = Point(goal), ["proposals"] = proposals };
                return goal;
            }
            areaContinuationPlans[label] = new JObject {
                ["moverId"] = mover.UniqueId, ["start"] = Point(start), ["proposals"] = proposals };
            throw new InvalidOperationException("Area fixture has no bounded traversable two-metre native continuation ray.");
        }
        private static float HorizontalDistance(UnityEngine.Vector3 first, UnityEngine.Vector3 second)
        {
            first.y = second.y = 0f;
            return UnityEngine.Vector3.Distance(first, second);
        }
        private static bool FiniteAreaPoint(UnityEngine.Vector3 point) =>
            FiniteAreaNumber(point.x) && FiniteAreaNumber(point.y) && FiniteAreaNumber(point.z);
        private static bool FiniteAreaNumber(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void EndAreaObservation()
        {
            if (!areaObserverAttached) return;
            persistence.SaveSnapshotStarting -= ObserveAreaAutosaveStarting;
            persistence.SaveSnapshotStaged -= ObserveAreaAutosaveStaged;
            areaObserverAttached = false;
        }
    }
}
