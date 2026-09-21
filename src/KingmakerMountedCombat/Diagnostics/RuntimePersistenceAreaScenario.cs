using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool AreaCase => request.Scenario == "persistence-p07-save" && request.PersistenceCase == "area-reload";
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
                Check(areaFrames > 0 && areaSuspensionObserved && ReferenceEquals(areaWorld, game.Player) &&
                    game.CurrentlyLoadedArea.AssetGuidThreadSafe == request.Fixture.Working.Area,
                    "P07-real-area-unload-and-same-native-world");
                rider = relationship.Rider; mount = relationship.Mount;
                Check(relationship.State == RelationshipState.Mounted && rider != null && mount != null &&
                    rider.UniqueId == areaRiderDebt.Id && mount.UniqueId == areaMountDebt.Id &&
                    rider.View.GetInstanceID() != areaRiderView && mount.View.GetInstanceID() != areaMountView,
                    "P07-same-actors-with-replaced-native-views");
                Check(game.State.Units.Count(u => u.UniqueId == rider.UniqueId) == 1 &&
                    game.State.Units.Count(u => u.UniqueId == mount.UniqueId) == 1, "P07-area-no-duplicate-native-actors");
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

        private JObject AreaDetail() => new JObject {
            ["area"] = Game.Instance.CurrentlyLoadedArea.AssetGuidThreadSafe,
            ["loadingFrames"] = areaFrames, ["suspensionObserved"] = areaSuspensionObserved,
            ["suspensions"] = persistence.AreaSuspensionCount, ["resumes"] = persistence.AreaResumeCount,
            ["pending"] = persistence.AreaTransitionPending, ["sameWorld"] = ReferenceEquals(areaWorld, Game.Instance.Player),
            ["riderView"] = rider.View.GetInstanceID(), ["mountView"] = mount.View.GetInstanceID(),
            ["nativeCastRequests"] = controls.NativeCastRequestCount
        };
    }
}
