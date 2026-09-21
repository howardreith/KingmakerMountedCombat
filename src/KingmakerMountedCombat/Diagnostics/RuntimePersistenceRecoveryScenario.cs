using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool RecoveryCase => request.Scenario == "persistence-p07-save";
        private bool recoveryContinuation;
        private int recoveryStage;
        private int recoveryFrames;
        private bool failedSaveCallback;
        private bool canceledLoadCallback;
        private IDisposable recoveryFault;
        private string recoveryGoodHash;
        private string recoveryRiderId;
        private string recoveryMountId;
        private Player recoveryWorld;
        private MountedSaveData recoveryData;
        private long recoveryWaitTicks;
        private SavedNativeActor recoveryRiderDebt;
        private SavedNativeActor recoveryMountDebt;

        private void AdvanceRecovery()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P07 recovery timed out at " + recoveryStage);
            var game = Game.Instance;
            stage = 500 + recoveryStage;
            if (recoveryStage == 2)
            {
                if (request.PersistenceCase == "cancel-wait")
                {
                    if (++recoveryFrames < 4) return;
                    Check(NativeDeferredSave.Waiting(LoadingProcess.Instance), "P07-cancel-real-native-selected-save-wait");
                    LoadingProcess.Instance.StopAll();
                }
                else if (persistence.FailedSaveCount == 0) return;
                if (LoadingProcess.Instance.IsLoadingInProcess) return;
                recoveryFault.Dispose(); recoveryFault = null;
                Check(ReferenceEquals(game.Player, recoveryWorld) && ReferenceEquals(persistence.LoadedData, recoveryData) &&
                    persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1,
                    "P07-failure-or-unstarted-load-cancellation-preserves-completed-world");
                Check(!failedSaveCallback && !canceledLoadCallback && persistence.SnapshotCount == 1 &&
                    persistence.FailedSaveCount == (request.PersistenceCase == "timeout" ? 1 : 0),
                    "P07-unwritten-operation-never-reports-save-or-load-success");
                Check(!NativeDeferredSave.Waiting(LoadingProcess.Instance) && !persistence.SaveSuspended &&
                    !NativePersistenceIsolation.HasPendingWrites && !game.IsPaused &&
                    game.CurrentMode == Kingmaker.GameModes.GameModeType.Default,
                    "P07-native-wait-input-and-save-scopes-recovered");
                var elapsed = (game.TimeController.GameTime.Ticks - recoveryWaitTicks) / (double)TimeSpan.TicksPerSecond;
                Check(elapsed > 0 && LegitimateContinuation(recoveryRiderDebt, MountedPersistenceService.CaptureActor(rider), elapsed) &&
                    LegitimateContinuation(recoveryMountDebt, MountedPersistenceService.CaptureActor(mount), elapsed),
                    "P07-wait-clock-advances-without-action-refund-or-tax");
                VerifyRecoveryControls();
                var saved = RecoveryArchive();
                Check(Hash(saved.FolderName) == recoveryGoodHash && persistence.SnapshotCount == 1,
                    "P07-previous-complete-archive-unchanged-after-unwritten-request");
                Write("recovery-unwritten-operation", RecoveryDetail(saved));
                RequestRecoveryLoad(saved);
                recoveryStage = 3;
                return;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites ||
                game.CurrentlyLoadedArea == null || game.CurrentMode != Kingmaker.GameModes.GameModeType.Default || !callback) return;
            var archive = RecoveryArchive();
            if (recoveryStage == 0)
            {
                Check(persistence.SnapshotCount == 1 && relationship.State == RelationshipState.Mounted,
                    "P07-first-real-native-save-completed");
                var data = NativeMountedSaveStorage.Read(archive.Saver);
                Check(data.Kind == MountedSaveReadKind.Current && data.Data.Mounted &&
                    data.Data.Rider.Id == rider.UniqueId && data.Data.Mount.Id == mount.UniqueId,
                    "P07-last-good-archive-contains-independent-pair");
                recoveryGoodHash = Hash(archive.FolderName);
                recoveryRiderId = rider.UniqueId; recoveryMountId = mount.UniqueId;
                Write("recovery-initial-write", RecoveryDetail(archive));
                RequestRecoveryLoad(archive);
                recoveryStage = 1;
                return;
            }
            if (recoveryStage == 1 || recoveryStage == 3)
            {
                if (++recoveryFrames < 10) return;
                Check(!ReferenceEquals(recoveryWorld, game.Player) && relationship.State == RelationshipState.Mounted &&
                    relationship.Rider.UniqueId == recoveryRiderId && relationship.Mount.UniqueId == recoveryMountId &&
                    persistence.SemanticRestoreCount == (recoveryStage == 1 ? 2 : 4) &&
                    persistence.PresentationRestoreCount == (recoveryStage == 1 ? 1 : 2) &&
                    controls.NativeCastRequestCount == 0, "P07-last-good-native-load-without-Mount-or-new-acquisition");
                rider = relationship.Rider; mount = relationship.Mount;
                VerifyRecoveryControls();
                Check(Hash(archive.FolderName) == recoveryGoodHash, "P07-native-load-preserves-last-good-bytes");
                if (recoveryStage == 3)
                {
                    Write("recovery-last-good-loaded", RecoveryDetail(archive));
                    callback = false;
                    game.SaveGame(archive, () => callback = true);
                    recoveryStage = 4;
                    return;
                }
                recoveryWorld = game.Player; recoveryData = persistence.LoadedData;
                recoveryWaitTicks = game.TimeController.GameTime.Ticks;
                recoveryRiderDebt = MountedPersistenceService.CaptureActor(rider);
                recoveryMountDebt = MountedPersistenceService.CaptureActor(mount);
                recoveryFault = persistence.ArmOwnedSaveWait(archive);
                callback = false;
                game.SaveGame(archive, () => failedSaveCallback = true);
                Check(NativeDeferredSave.Waiting(LoadingProcess.Instance) && persistence.SnapshotCount == 1,
                    "P07-fault-is-one-owned-native-request-before-snapshot");
                if (request.PersistenceCase == "cancel-wait")
                {
                    var queued = game.SaveManager.LoadRoutine(archive, false);
                    LoadingProcess.Instance.StartLoadingProcess(queued, () => canceledLoadCallback = true);
                    Check(LoadingProcess.Instance.QueuedNames.Count() == 1 &&
                        ReferenceEquals(persistence.LoadedData, recoveryData), "P07-unstarted-native-load-retains-current-save-semantics");
                }
                Write("recovery-wait-started", RecoveryDetail(archive));
                recoveryFrames = 0; recoveryStage = 2;
                return;
            }
            if (recoveryStage == 4)
            {
                var read = NativeMountedSaveStorage.Read(archive.Saver);
                Check(persistence.SnapshotCount == 2 && read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == recoveryRiderId && read.Data.Mount.Id == recoveryMountId &&
                    Hash(archive.FolderName) != recoveryGoodHash && !failedSaveCallback && !canceledLoadCallback,
                    "P07-subsequent-real-native-save-completes-after-recovery");
                VerifyRecoveryControls();
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 2, ["path"] = archive.FolderName, ["sha256"] = Hash(archive.FolderName),
                    ["length"] = new FileInfo(archive.FolderName).Length, ["nativeType"] = archive.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = archive.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                recoveryContinuation = true; stage = 2;
            }
        }

        private SaveInfo RecoveryArchive()
        {
            var save = Game.Instance.SaveManager.Single(s => s.Name == "KMC_P01");
            Check(save.Type == SaveInfo.SaveType.Manual && save.FileName == "Manual_300_KMC_P01.zks" &&
                save.HasFileOnDisk && save.OperationState == SaveInfo.StateType.None, "P07-exact-complete-native-manual-archive");
            return save;
        }

        private void RequestRecoveryLoad(SaveInfo save)
        {
            recoveryWorld = Game.Instance.Player;
            callback = false; recoveryFrames = 0;
            Game.Instance.SaveManager.AddCallbackAfterLoad(() => callback = true);
            Game.Instance.LoadGameFromMainMenu(save);
        }

        private void VerifyRecoveryControls()
        {
            var current = controls.CaptureSnapshot();
            Check(current.ExactFactCount == beforeControls.ExactFactCount && current.DuplicateFactCount == 0 &&
                current.ManagedHotbarSlotCount == beforeControls.ManagedHotbarSlotCount && !current.SerializationSuspended &&
                relationship.State == RelationshipState.Mounted, "P07-owned-controls-and-relationship-recover-once");
        }

        private JObject RecoveryDetail(SaveInfo save) => new JObject {
            ["case"] = request.PersistenceCase, ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName),
            ["length"] = new FileInfo(save.FolderName).Length, ["nativeCallback"] = callback,
            ["failedSaveCallback"] = failedSaveCallback, ["canceledLoadCallback"] = canceledLoadCallback,
            ["snapshots"] = persistence.SnapshotCount, ["failedSaves"] = persistence.FailedSaveCount,
            ["nativeWorldDisposals"] = persistence.NativeWorldDisposalCount,
            ["previousHash"] = recoveryGoodHash, ["waitGameTicks"] = recoveryWaitTicks
        };
    }
}
