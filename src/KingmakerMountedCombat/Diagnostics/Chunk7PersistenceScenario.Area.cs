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
        private string ExpectedArchiveArea => AreaAutosaveCase ? request.PersistenceAreaTarget?.Area : request.Fixture.Working.Area;
        private Player areaWorld;
        private bool areaObservedSuspension, areaObserverAttached;
        private int areaLoadingTicks;
        private long areaCastBaseline, areaSnapshotBaseline;
        private string areaObserverError;
        private JObject areaBefore, areaHeaderStarting, areaHeaderStaged;
        private readonly Dictionary<string, int> areaViewIds = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> areaMovementIds = new Dictionary<string, int>(StringComparer.Ordinal);
        private SavedNativeActor[] areaActorDebt;
        private long areaNativeTicks;

        // Invoked after both native Mount shells settle and owned slots are bound.
        // The engine's authored transition is the sole source of the autosave.
        private void BeginAreaAutosave()
        {
            var game = Game.Instance; var target = request.PersistenceAreaTarget;
            Require(AreaAutosaveCase && !Cold && target != null && target.AutoSaveMode == "AfterEntry" &&
                target.Area != request.Fixture.Working.Area && game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.Fixture.Working.Area,
                "Area autosave requires its exact declared destination and AfterEntry policy.");
            Require(!game.Player.IsInCombat && !LoadingProcess.Instance.IsLoadingInProcess && !LoadingProcess.Instance.QueuedNames.Any() &&
                !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !persistence.AreaTransitionPending &&
                persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 && !NativePersistenceIsolation.HasPendingWrites,
                "Area autosave started across an unsettled native world boundary.");
            AssertOwners(); AssertSlots();
            Require(actors.Length == 4 && actors.All(a => a.Commands.Empty && a.View?.AgentASP != null && a.IsInState && a.IsInGame),
                "Area autosave lacks four exact idle actors and live native views.");
            var enter = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(target.EnterPoint);
            Require(enter != null && enter.AssetGuidThreadSafe == target.EnterPoint && enter.Area != null &&
                enter.Area.AssetGuidThreadSafe == target.Area && enter.Area != game.CurrentlyLoadedArea,
                "Area autosave cannot resolve the exact authorized native enter point.");
            Require(SettingsRoot.Instance.AutosaveEnabled.CurrentValue && game.SaveManager.IsSaveAllowed() &&
                !game.SaveManager.Any(s => s.Type == SaveInfo.SaveType.Auto && s.IsActuallySaved),
                "Native AfterEntry autosave is disabled, refused or would rotate an existing slot.");
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
            castBefore = areaCastBaseline; snapshotBefore = areaSnapshotBaseline; callback = false;
            areaBefore = CaptureAreaWorld("before-native-transfer");
            evidence["areaTransfer"] = new JObject { ["mode"] = target.AutoSaveMode, ["enterPoint"] = target.EnterPoint,
                ["sourceArea"] = request.Fixture.Working.Area, ["destinationArea"] = target.Area, ["before"] = areaBefore,
                ["nativeSaveCallbackExpected"] = false, ["explicitSaveGameDispatches"] = 0 };
            persistence.SaveSnapshotStarting += ObserveAreaAutosaveStarting;
            persistence.SaveSnapshotStaged += ObserveAreaAutosaveStaged;
            areaObserverAttached = true;
            Next(30);
            game.LoadArea(enter, AutoSaveMode.AfterEntry);
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
                return true;
            }
            if (persistence.AreaResumeCount < 2 || persistence.AreaTransitionPending || NativePersistenceIsolation.HasPendingWrites ||
                persistence.HasActiveSaveScope || persistence.HasSaveAdmissionFence) return true;
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
                persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == areaCastBaseline &&
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
                if (areaHeaderStarting != null) throw new InvalidOperationException("Area case observed a second native header start.");
                areaHeaderStarting = CaptureAreaWorld("native-header-starting");
                AssertOwners(); AssertAreaRestoredViews();
            }
            catch (Exception error) { areaObserverError = areaObserverError ?? error.ToString(); }
        }
        private void ObserveAreaAutosaveStaged()
        {
            try
            {
                if (areaHeaderStaged != null) throw new InvalidOperationException("Area case observed a second native header stage.");
                areaHeaderStaged = CaptureAreaWorld("native-header-staged");
                AssertOwners(); AssertAreaRestoredViews();
            }
            catch (Exception error) { areaObserverError = areaObserverError ?? error.ToString(); }
        }
        private JObject CaptureAreaWorld(string label)
        {
            var game = Game.Instance;
            var row = Capture(label);
            row["area"] = game?.CurrentlyLoadedArea?.AssetGuidThreadSafe;
            row["sameWorld"] = ReferenceEquals(areaWorld, game?.Player);
            row["suspensions"] = persistence.AreaSuspensionCount; row["resumes"] = persistence.AreaResumeCount;
            row["pending"] = persistence.AreaTransitionPending; row["activeOwners"] = world.Registry.ActiveCount;
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
                    ["invariant"] = runtime?.ValidateMountedInvariants(), ["stockEnabled"] = rider.View?.AgentASP?.enabled,
                    ["mountId"] = relationship?.Mount?.UniqueId, ["mountStockEnabled"] = relationship?.Mount?.View?.AgentASP?.enabled,
                    ["mountOverride"] = relationship?.Mount?.View?.AgentOverride == null ? 0 : relationship.Mount.View.AgentOverride.GetInstanceID(),
                    ["mountOverrideComponents"] = new JArray(relationship?.Mount?.View == null ? new int[0] : relationship.Mount.View.GetComponents<RiderMovementAgent>().Select(a => a.GetInstanceID()).ToArray()),
                    ["riderViewBoundToOwner"] = relationship?.IsExactCapturedView(rider),
                    ["mountViewBoundToOwner"] = relationship?.IsExactCapturedView(relationship.Mount) };
            }));
            return row;
        }
        private void AssertAreaRestoredViews()
        {
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
                    relation.IsExactCapturedView(relation.Mount) && runtime.ValidateMountedInvariants() == null,
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
        private void EndAreaObservation()
        {
            if (!areaObserverAttached) return;
            persistence.SaveSnapshotStarting -= ObserveAreaAutosaveStarting;
            persistence.SaveSnapshotStaged -= ObserveAreaAutosaveStaged;
            areaObserverAttached = false;
        }
    }
}
