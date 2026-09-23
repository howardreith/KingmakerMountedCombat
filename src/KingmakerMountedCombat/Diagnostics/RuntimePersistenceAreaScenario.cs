using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        // Installed contract, verified read-only against Assembly-CSharp MVID
        // 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7: ReloadArea 06000CD6 calls
        // LoadArea 06000CD5 with a null saveInfo, and LoadArea passes
        // (saveInfo != null) as UnloadEntitiesCoroutine 06008096's
        // unloadCrossScene 04008DA5. That iterator always destroys DynamicRoot
        // but destroys CrossSceneRoot only when the flag is true, and
        // UnloadAreaCoroutine 06008095 destroys only cross-scene units whose
        // master left the party. An ordinary transfer therefore retains the
        // party rider and its pet mount together with their exact native views;
        // replacement is the save-load contract, not this one.
        private const string RetainedNativeView = "retained";
        private const string ReplacedNativeView = "replaced";
        private const string MissingNativeView = "missing";

        private bool AreaCase => request.Scenario == "persistence-p07-save" && request.PersistenceCase == "area-reload";
        private string ExpectedAreaViewDisposition => RetainedNativeView;
        private bool areaContinuation, areaSuspensionObserved;
        private int areaStage, areaFrames, areaRiderView, areaMountView;
        private Player areaWorld;
        private string areaGoodHash;
        private SavedNativeActor areaRiderDebt, areaMountDebt;
        private long areaGameTicks;

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
                if (!callback) return;
                var archive = RecoveryArchive();
                var read = NativeMountedSaveStorage.Read(archive.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    persistence.SnapshotCount == 1 && persistence.AreaSuspensionCount == 0 &&
                    persistence.AreaResumeCount == 0, "P07-area-initial-real-mounted-save");
                areaGoodHash = Hash(archive.FolderName);
                Write("area-initial-write", new JObject { ["sha256"] = areaGoodHash, ["path"] = archive.FolderName });
                areaWorld = game.Player;
                Check(IsBoundNativeView(rider) && IsBoundNativeView(mount), "P07-area-baseline-native-views-bound");
                areaRiderView = rider.View.GetInstanceID(); areaMountView = mount.View.GetInstanceID();
                areaRiderDebt = MountedPersistenceService.CaptureActor(rider);
                areaMountDebt = MountedPersistenceService.CaptureActor(mount);
                areaGameTicks = game.TimeController.GameTime.Ticks;
                Write("area-reload-requested", AreaDetail());
                game.ReloadArea(); // Exact native 06000CD6; real unload/replacement.
                areaStage = 1;
                return;
            }
            if (areaStage == 1)
            {
                if (persistence.AreaResumeCount == 0) return; // Queue dispatch is asynchronous.
                rider = relationship.Rider; mount = relationship.Mount;
                // Observation precedes qualification: a failing row must still keep
                // the measured post-reload view identities that decide the case.
                Write("area-reload-observed", AreaDetail());
                Check(areaFrames > 0 && areaSuspensionObserved && ReferenceEquals(areaWorld, game.Player) &&
                    game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.Fixture.Working.Area,
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
                persistence.RestoreAreaPair(); persistence.RestoreAreaPair();
                Check(persistence.AreaResumeCount == 1 && Hash(RecoveryArchive().FolderName) == areaGoodHash,
                    "P07-area-duplicate-ready-callback-no-op-source-unchanged");
                Write("area-reload-complete", AreaDetail());
                callback = false;
                game.SaveGame(RecoveryArchive(), () => callback = true);
                areaStage = 2;
                return;
            }
            if (areaStage == 2)
            {
                if (!callback) return;
                var archive = RecoveryArchive();
                var read = NativeMountedSaveStorage.Read(archive.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId &&
                    persistence.SnapshotCount == 2 && Hash(archive.FolderName) != areaGoodHash &&
                    !game.IsPaused && !persistence.AreaTransitionPending, "P07-real-post-area-write-resumes-native-play");
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 2, ["path"] = archive.FolderName, ["sha256"] = Hash(archive.FolderName),
                    ["length"] = new FileInfo(archive.FolderName).Length, ["nativeType"] = archive.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = archive.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                areaContinuation = true; stage = 2;
            }
        }

        private JObject AreaDetail()
        {
            var game = Game.Instance;
            var loading = LoadingProcess.Instance;
            var mounted = relationship.State == RelationshipState.Mounted;
            return new JObject {
                ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["loadingFrames"] = areaFrames, ["suspensionObserved"] = areaSuspensionObserved,
                ["suspensions"] = persistence.AreaSuspensionCount, ["resumes"] = persistence.AreaResumeCount,
                ["pending"] = persistence.AreaTransitionPending, ["sameWorld"] = ReferenceEquals(areaWorld, game.Player),
                ["riderView"] = AreaViewId(relationship.Rider), ["mountView"] = AreaViewId(relationship.Mount),
                ["nativeCastRequests"] = controls.NativeCastRequestCount,
                ["expectedViewDisposition"] = ExpectedAreaViewDisposition,
                ["loadingInProcess"] = loading.IsLoadingInProcess,
                ["queuedLoads"] = loading.QueuedNames.Count(),
                ["deferredSaveWaiting"] = NativeDeferredSave.Waiting(loading),
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
