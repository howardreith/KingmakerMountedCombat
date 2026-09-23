using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // Installed contract, verified read-only against Assembly-CSharp MVID
        // 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7: ReloadArea 06000CD6 and the public
        // LoadArea 06000CC9 both reach LoadArea 06000CD5 with a null saveInfo, and
        // LoadArea passes (saveInfo != null) as SceneLoader.UnloadEntitiesCoroutine
        // 06008096's unloadCrossScene 04008DA5. That iterator always destroys
        // DynamicRoot but destroys CrossSceneRoot only when the flag is true, and
        // UnloadAreaCoroutine 06008095 destroys only cross-scene units whose master
        // left the party. Every ordinary transfer therefore retains the party rider
        // and its pet mount together with their exact native views; replacement is
        // the save-load contract.
        private const string RetainedNativeView = "retained";
        private const string ReplacedNativeView = "replaced";
        private const string MissingNativeView = "missing";

        private bool AreaReloadCase => request.Scenario == "persistence-p07-save" && request.PersistenceCase == "area-reload";
        private bool CrossAreaCase => request.Scenario == "persistence-p07-save" &&
            RuntimeRequest.IsCrossAreaCase(request.PersistenceCase);
        private bool AreaCase => AreaReloadCase || CrossAreaCase;
        // True in both processes of a declared transfer: the cold load also opens
        // in the destination, where the party arrived at one enter point.
        private bool CrossAreaFixture => request.PersistenceAreaTarget != null;

        // A cold process loading the transition autosave itself. AfterEntry
        // committed in the destination after restoration; BeforeExit committed
        // in the departure area before suspension. Each must open in its own
        // world with no inherited transfer state from the source process.
        private bool AreaAutoColdCase => request.Scenario == "persistence-p07-load" &&
            RuntimeRequest.IsTransitionAutoCase(request.PersistenceCase);
        private string ExpectedAutoColdArea => request.PersistenceCase == "area-cross-entry-auto"
            ? request.PersistenceAreaTarget.Area : request.Fixture.Working.Area;

        private void QualifyTransitionAutoColdLoad()
        {
            var game = Game.Instance;
            Check(game.CurrentlyLoadedArea.AssetGuidThreadSafe == ExpectedAutoColdArea &&
                persistence.LoadedData.AreaId == ExpectedAutoColdArea,
                "P07-auto-cold-opens-the-area-its-own-archive-captured");
            Check(persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 &&
                !persistence.AreaTransitionPending,
                "P07-auto-cold-inherits-no-transfer-state-from-its-source-process");
            // Compared against the archive's own native clock, never the source
            // run's end state after further travel or combat.
            var elapsed = Math.Max(0, (game.TimeController.GameTime.Ticks -
                persistence.LoadedData.GameTimeTicks) / (double)TimeSpan.TicksPerSecond);
            Check(LegitimateContinuation(persistence.LoadedData.Rider, MountedPersistenceService.CaptureActor(rider), elapsed) &&
                LegitimateContinuation(persistence.LoadedData.Mount, MountedPersistenceService.CaptureActor(mount), elapsed),
                "P07-auto-cold-debt-matches-its-own-archive");
            Write("auto-cold-loaded", new JObject {
                ["case"] = request.PersistenceCase,
                ["autoSaveMode"] = request.PersistenceAreaTarget.AutoSaveMode,
                ["expectedArea"] = ExpectedAutoColdArea,
                ["loadedArea"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["archiveArea"] = persistence.LoadedData.AreaId,
                ["archiveCampaign"] = persistence.LoadedData.CampaignId,
                ["sourceSha256"] = request.PersistenceLoad.Sha256,
                ["sourceFileName"] = request.PersistenceLoad.FileName,
                ["suspensions"] = persistence.AreaSuspensionCount,
                ["resumes"] = persistence.AreaResumeCount,
                ["pending"] = persistence.AreaTransitionPending,
                ["riderActor"] = ColdActorDetail(persistence.LoadedData.Rider, true),
                ["mountActor"] = ColdActorDetail(persistence.LoadedData.Mount, false)
            });
        }

        private int autoColdWaitFrames, autoColdWaitRows;

        // The first attempt requested an authorized write that never completed
        // and only reported a stage timeout. Record the actual native writer
        // state a bounded number of times so the boundary names itself.
        private void ObserveAutoColdWriteWait()
        {
            autoColdWaitFrames++;
            if (autoColdWaitFrames % 120 != 1 || autoColdWaitRows >= 4) return;
            autoColdWaitRows++;
            var game = Game.Instance;
            var loading = LoadingProcess.Instance;
            var candidate = game.SaveManager.FirstOrDefault(s => s.Name == "KMC_P01");
            Write("auto-cold-write-waiting", new JObject {
                ["frames"] = autoColdWaitFrames,
                ["callback"] = callback,
                ["pendingWrites"] = NativePersistenceIsolation.HasPendingWrites,
                ["snapshots"] = persistence.SnapshotCount,
                ["failedSaves"] = persistence.FailedSaveCount,
                ["loadingInProcess"] = loading.IsLoadingInProcess,
                ["queuedLoads"] = loading.QueuedNames.Count(),
                ["deferredSaveWaiting"] = NativeDeferredSave.Waiting(loading),
                ["paused"] = game.IsPaused,
                ["mode"] = game.CurrentMode.ToString(),
                ["saveAllowed"] = game.SaveManager.IsSaveAllowed(),
                ["saveCount"] = game.SaveManager.Count(),
                ["candidateName"] = candidate == null ? null : candidate.Name,
                ["candidateFile"] = candidate == null ? null : candidate.FileName,
                ["candidateState"] = candidate == null ? null : candidate.OperationState.ToString(),
                ["candidateOnDisk"] = candidate != null && candidate.HasFileOnDisk
            });
        }

        // A cold load reconstructs the world, so Unity instance identity is not
        // comparable across processes. Only validity and unique binding are
        // claimed here; no retained/replaced disposition is asserted.
        private JObject ColdActorDetail(SavedNativeActor saved, bool isRider)
        {
            var id = saved == null ? null : saved.Id;
            var matches = id == null ? new UnitEntityData[0] :
                Game.Instance.State.Units.Where(u => u.UniqueId == id).ToArray();
            var actor = matches.Length == 1 ? matches[0] : null;
            var view = actor == null ? null : actor.View;
            var alive = view != null;
            return new JObject {
                ["id"] = id,
                ["nativeActorCount"] = matches.Length,
                ["viewId"] = alive ? new JValue(view.GetInstanceID()) : JValue.CreateNull(),
                ["viewAlive"] = alive,
                ["viewBound"] = IsBoundNativeView(actor),
                ["viewExactForPair"] = actor != null && relationship.IsExactCapturedView(actor),
                ["boundToRelationship"] = actor != null &&
                    ReferenceEquals(actor, isRider ? relationship.Rider : relationship.Mount),
                ["viewDisposition"] = "cold-world"
            };
        }
        private bool AfterEntryAutosave => request.PersistenceAreaTarget?.AutoSaveMode == "AfterEntry";
        private string ExpectedAreaViewDisposition => RetainedNativeView;
        private string ExpectedAreaDestination => CrossAreaCase ?
            request.PersistenceAreaTarget.Area : request.Fixture.Working.Area;
        private bool areaContinuation, areaSuspensionObserved;
        private int areaStage, areaFrames, areaRiderView, areaMountView;
        private Player areaWorld;
        private string areaGoodHash, areaSourceArea, areaAutosaveHash;
        private SavedNativeActor areaRiderDebt, areaMountDebt;
        private long areaGameTicks;
        private JObject areaAutosaveBarrier;

        // Fires on the game thread at the native header barrier, inside the
        // engine's own autosave. These counters are the ordering evidence: an
        // after-entry autosave must already see the restored pair, and a
        // before-exit autosave must still see it mounted in the departure area.
        private void ObserveAreaTransitionSnapshot()
        {
            if (areaAutosaveBarrier != null) return;
            var game = Game.Instance;
            areaAutosaveBarrier = new JObject {
                ["snapshots"] = persistence.SnapshotCount,
                ["suspensions"] = persistence.AreaSuspensionCount,
                ["resumes"] = persistence.AreaResumeCount,
                ["pending"] = persistence.AreaTransitionPending,
                ["relationship"] = relationship.State.ToString(),
                ["area"] = game?.CurrentlyLoadedArea?.AssetGuidThreadSafe,
                ["riderId"] = relationship.Rider?.UniqueId,
                ["mountId"] = relationship.Mount?.UniqueId,
                ["riderView"] = AreaViewId(relationship.Rider),
                ["mountView"] = AreaViewId(relationship.Mount)
            };
        }

        private void AdvanceArea()
        {
            if (clock.Elapsed.TotalSeconds > 150) throw new InvalidOperationException("P07 native area stage timed out: " + areaStage);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess)
            {
                areaFrames++;
                if (persistence.AreaSuspensionCount == 1 && relationship.State == RelationshipState.Unmounted)
                    areaSuspensionObserved = true;
                return;
            }
            if (areaStage == 0)
            {
                if (CrossAreaCase) { BeginCrossAreaTransfer(); return; }
                if (!callback) return;
                var archive = RecoveryArchive();
                var read = NativeMountedSaveStorage.Read(archive.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    persistence.SnapshotCount == 1 && persistence.AreaSuspensionCount == 0 &&
                    persistence.AreaResumeCount == 0, "P07-area-initial-real-mounted-save");
                areaGoodHash = Hash(archive.FolderName);
                Write("area-initial-write", new JObject { ["sha256"] = areaGoodHash, ["path"] = archive.FolderName });
                CaptureAreaBaseline();
                Write("area-reload-requested", AreaDetail());
                game.ReloadArea(); // Exact native 06000CD6; real unload/replacement.
                areaStage = 1;
                return;
            }
            if (areaStage == 1)
            {
                if (persistence.AreaResumeCount == 0) return; // Queue dispatch is asynchronous.
                if (CrossAreaCase && (NativePersistenceIsolation.HasPendingWrites || NativeAutosaveArchive() == null)) return;
                rider = relationship.Rider; mount = relationship.Mount;
                // Observation precedes qualification: a failing row must still keep
                // the measured post-transfer view identities that decide the case.
                Write("area-reload-observed", AreaDetail());
                Check(areaFrames > 0 && areaSuspensionObserved && ReferenceEquals(areaWorld, game.Player) &&
                    game.CurrentlyLoadedArea.AssetGuidThreadSafe == ExpectedAreaDestination &&
                    game.CurrentMode == GameModeType.Default,
                    "P07-real-area-unload-and-same-native-world");
                Check(relationship.State == RelationshipState.Mounted && rider != null && mount != null &&
                    rider.UniqueId == areaRiderDebt.Id && mount.UniqueId == areaMountDebt.Id,
                    "P07-same-actors-after-native-area-placement");
                Check(game.State.Units.Count(u => u.UniqueId == rider.UniqueId) == 1 &&
                    game.State.Units.Count(u => u.UniqueId == mount.UniqueId) == 1, "P07-area-no-duplicate-native-actors");
                // Retained views are the native outcome here; they still have to be
                // the live, bound, singly owned views this pair actually uses.
                Check(AreaViewDisposition(rider, areaRiderView) == ExpectedAreaViewDisposition &&
                    AreaViewDisposition(mount, areaMountView) == ExpectedAreaViewDisposition &&
                    IsBoundNativeView(rider) && IsBoundNativeView(mount) &&
                    relationship.IsExactCapturedView(rider) && relationship.IsExactCapturedView(mount) &&
                    relationship.Runtime.ValidateMountedInvariants() == null,
                    "P07-area-native-view-disposition-and-single-owned-attachment");
                var elapsed = Math.Max(0, (game.TimeController.GameTime.Ticks - areaGameTicks) / (double)TimeSpan.TicksPerSecond);
                Check(LegitimateContinuation(areaRiderDebt, MountedPersistenceService.CaptureActor(rider), elapsed) &&
                    LegitimateContinuation(areaMountDebt, MountedPersistenceService.CaptureActor(mount), elapsed),
                    "P07-area-retains-legitimate-native-debt");
                Check(persistence.AreaSuspensionCount == 1 && persistence.AreaResumeCount == 1 &&
                    !persistence.AreaTransitionPending && persistence.SemanticRestoreCount == 0 &&
                    persistence.PresentationRestoreCount == 0 && controls.NativeCastRequestCount == 0,
                    "P07-area-no-load-debt-replay-or-mount-cast");
                controls.Update();
                var after = controls.CaptureSnapshot();
                Check(after.ExactFactCount == beforeControls.ExactFactCount && after.DuplicateFactCount == 0 &&
                    after.ManagedHotbarSlotCount == beforeControls.ManagedHotbarSlotCount &&
                    !after.SerializationSuspended, "P07-area-controls-and-owned-slots-once");
                if (CrossAreaCase) QualifyNativeAreaAutosave();
                var guardHash = CrossAreaCase ? areaAutosaveHash : areaGoodHash;
                var guardPath = CrossAreaCase ? NativeAutosaveArchive().FolderName : RecoveryArchive().FolderName;
                persistence.RestoreAreaPair(); persistence.RestoreAreaPair();
                Check(persistence.AreaResumeCount == 1 && Hash(guardPath) == guardHash,
                    "P07-area-duplicate-ready-callback-no-op-source-unchanged");
                Write("area-reload-complete", AreaDetail());
                callback = false;
                // The destination-area manual leaf is a new owned request; the
                // same-area case still overwrites its exact existing archive.
                game.SaveGame(CrossAreaCase ? game.SaveManager.CreateNewSave("KMC_P01") : RecoveryArchive(),
                    () => callback = true);
                areaStage = 2;
                return;
            }
            if (areaStage == 2)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                var archive = RecoveryArchive();
                var read = NativeMountedSaveStorage.Read(archive.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId &&
                    read.Data.AreaId == ExpectedAreaDestination &&
                    persistence.SnapshotCount == 2 && Hash(archive.FolderName) != (CrossAreaCase ? areaAutosaveHash : areaGoodHash) &&
                    !game.IsPaused && !persistence.AreaTransitionPending, "P07-real-post-area-write-resumes-native-play");
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 2, ["path"] = archive.FolderName, ["sha256"] = Hash(archive.FolderName),
                    ["length"] = new FileInfo(archive.FolderName).Length, ["nativeType"] = archive.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = archive.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                areaContinuation = true; stage = 2;
            }
        }

        // An ordinary authored transition, not a synthetic call: the module's only
        // non-literal AutoSaveMode sources are blueprint fields such as
        // AreaTransition.AutoSaveMode 0400123A, so both BeforeExit and AfterEntry
        // are real in-world transition modes reaching the public 06000CC9 entry.
        private void BeginCrossAreaTransfer()
        {
            var game = Game.Instance;
            var target = request.PersistenceAreaTarget;
            areaSourceArea = game.CurrentlyLoadedArea.AssetGuidThreadSafe;
            Check(persistence.SnapshotCount == 0 && persistence.AreaSuspensionCount == 0 &&
                persistence.AreaResumeCount == 0 && !persistence.AreaTransitionPending &&
                areaSourceArea == request.Fixture.Working.Area, "P07-cross-area-idle-before-native-transfer");
            var enter = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(target.EnterPoint);
            Check(enter != null && enter.AssetGuidThreadSafe == target.EnterPoint && enter.Area != null &&
                enter.Area.AssetGuidThreadSafe == target.Area && target.Area != areaSourceArea &&
                enter.Area != game.CurrentlyLoadedArea, "P07-cross-area-exact-native-enter-point");
            NativePersistenceIsolation.EnableNativeSlotRotation();
            Check(Kingmaker.UI.SettingsUI.SettingsRoot.Instance.AutosaveEnabled.CurrentValue &&
                game.SaveManager.IsSaveAllowed() &&
                !game.SaveManager.Any(s => s.Type == SaveInfo.SaveType.Auto && s.IsActuallySaved),
                "P07-cross-area-native-autosave-admitted-without-existing-slot");
            CaptureAreaBaseline();
            Write("area-reload-requested", AreaDetail());
            game.LoadArea(enter, AfterEntryAutosave ? AutoSaveMode.AfterEntry : AutoSaveMode.BeforeExit);
            areaStage = 1;
        }

        private void CaptureAreaBaseline()
        {
            var game = Game.Instance;
            areaWorld = game.Player;
            Check(IsBoundNativeView(rider) && IsBoundNativeView(mount), "P07-area-baseline-native-views-bound");
            areaRiderView = rider.View.GetInstanceID(); areaMountView = mount.View.GetInstanceID();
            areaRiderDebt = MountedPersistenceService.CaptureActor(rider);
            areaMountDebt = MountedPersistenceService.CaptureActor(mount);
            areaGameTicks = game.TimeController.GameTime.Ticks;
        }

        private SaveInfo NativeAutosaveArchive()
        {
            var name = SlotName(SaveInfo.SaveType.Auto);
            var save = Game.Instance.SaveManager.SingleOrDefault(s => s.Type == SaveInfo.SaveType.Auto && s.Name == name);
            return save != null && save.HasFileOnDisk && save.OperationState == SaveInfo.StateType.None ? save : null;
        }

        private void QualifyNativeAreaAutosave()
        {
            var archive = NativeAutosaveArchive();
            Check(archive != null && archive.FileName == "Auto_1.zks" && persistence.SnapshotCount == 1 &&
                persistence.FailedSaveCount == 0, "P07-cross-area-exact-single-native-autosave");
            areaAutosaveHash = Hash(archive.FolderName);
            var read = NativeMountedSaveStorage.Read(archive.Saver);
            // The before-exit autosave must capture the still-mounted departure
            // area; the after-entry one must already contain the restored pair.
            var expectedArea = AfterEntryAutosave ? ExpectedAreaDestination : areaSourceArea;
            Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                read.Data.Rider.Id == areaRiderDebt.Id && read.Data.Mount.Id == areaMountDebt.Id &&
                read.Data.AreaId == expectedArea && read.Data.CampaignId == areaWorld.GameId,
                "P07-cross-area-autosave-carries-exact-mounted-pair");
            var barrier = areaAutosaveBarrier;
            Check(barrier != null && (string)barrier["relationship"] == "Mounted" &&
                (string)barrier["area"] == expectedArea && (int)barrier["snapshots"] == 1 &&
                (int)barrier["suspensions"] == (AfterEntryAutosave ? 1 : 0) &&
                (int)barrier["resumes"] == (AfterEntryAutosave ? 1 : 0) &&
                (string)barrier["riderId"] == areaRiderDebt.Id && (string)barrier["mountId"] == areaMountDebt.Id,
                "P07-cross-area-autosave-barrier-follows-native-restoration-order");
            Write("area-native-autosave", new JObject {
                ["mode"] = request.PersistenceAreaTarget.AutoSaveMode, ["path"] = archive.FolderName,
                ["sha256"] = areaAutosaveHash, ["length"] = new FileInfo(archive.FolderName).Length,
                ["nativeType"] = archive.Type.ToString(), ["expectedArea"] = expectedArea,
                ["barrier"] = barrier, ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
        }

        private JObject AreaDetail()
        {
            var game = Game.Instance;
            var loading = LoadingProcess.Instance;
            var mounted = relationship.State == RelationshipState.Mounted;
            return new JObject {
                ["case"] = request.PersistenceCase,
                ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["expectedArea"] = ExpectedAreaDestination,
                ["sourceArea"] = areaSourceArea,
                ["autoSaveMode"] = request.PersistenceAreaTarget?.AutoSaveMode,
                ["loadingFrames"] = areaFrames, ["suspensionObserved"] = areaSuspensionObserved,
                ["suspensions"] = persistence.AreaSuspensionCount, ["resumes"] = persistence.AreaResumeCount,
                ["pending"] = persistence.AreaTransitionPending, ["sameWorld"] = ReferenceEquals(areaWorld, game.Player),
                ["riderView"] = AreaViewId(relationship.Rider), ["mountView"] = AreaViewId(relationship.Mount),
                ["nativeCastRequests"] = controls.NativeCastRequestCount,
                ["expectedViewDisposition"] = ExpectedAreaViewDisposition,
                ["loadingInProcess"] = loading.IsLoadingInProcess,
                ["queuedLoads"] = loading.QueuedNames.Count(),
                ["deferredSaveWaiting"] = NativeDeferredSave.Waiting(loading),
                ["snapshots"] = persistence.SnapshotCount,
                ["mountedInvariant"] = mounted ? relationship.Runtime.ValidateMountedInvariants() : null,
                ["presentation"] = relationship.CapturePresentationObservation(false),
                ["riderActor"] = AreaActorDetail(areaRiderDebt, areaRiderView, true),
                ["mountActor"] = AreaActorDetail(areaMountDebt, areaMountView, false)
            };
        }

        // Bounded, read-only and null-safe: it resolves the expected actor from
        // the native world rather than from the relationship, so a failed
        // restoration still records what the engine actually produced.
        private JObject AreaActorDetail(SavedNativeActor baseline, int baselineViewId, bool isRider)
        {
            var id = baseline == null ? null : baseline.Id;
            var matches = id == null ? new UnitEntityData[0] :
                Game.Instance.State.Units.Where(u => u.UniqueId == id).ToArray();
            var actor = matches.Length == 1 ? matches[0] : null;
            var view = actor == null ? null : actor.View;
            var alive = view != null;
            return new JObject {
                ["id"] = id,
                ["nativeActorCount"] = matches.Length,
                ["baselineViewId"] = baselineViewId,
                ["viewId"] = alive ? new JValue(view.GetInstanceID()) : JValue.CreateNull(),
                ["viewAlive"] = alive,
                ["viewBound"] = IsBoundNativeView(actor),
                ["viewDisposition"] = AreaViewDisposition(actor, baselineViewId),
                ["viewExactForPair"] = actor != null && relationship.IsExactCapturedView(actor),
                ["boundToRelationship"] = actor != null &&
                    ReferenceEquals(actor, isRider ? relationship.Rider : relationship.Mount)
            };
        }

        private static JValue AreaViewId(UnitEntityData actor)
        {
            var view = actor == null ? null : actor.View;
            return view == null ? JValue.CreateNull() : new JValue(view.GetInstanceID());
        }

        private static string AreaViewDisposition(UnitEntityData actor, int baselineViewId)
        {
            var view = actor == null ? null : actor.View;
            if (view == null) return MissingNativeView;
            return view.GetInstanceID() == baselineViewId ? RetainedNativeView : ReplacedNativeView;
        }

        private static bool IsBoundNativeView(UnitEntityData actor)
        {
            var view = actor == null ? null : actor.View;
            return view != null && ReferenceEquals(view.EntityData, actor);
        }
    }
}
