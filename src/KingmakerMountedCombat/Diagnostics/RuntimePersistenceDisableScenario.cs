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
        private bool DisableCase => RecoveryCase && request.PersistenceCase == "disable-reenable";
        private int disableStage;
        private int disableFrames;
        private string disableRiderId;
        private string disableMountId;
        private int disableFactsMounted;
        private int disableSlotsMounted;
        private int disableFactsUnmounted;
        private long disableCastsBefore;
        private bool disableSecondMountRejected;
        private bool disableRefusedDuringSave;
        private bool disableSuspendedAtProbe;
        private bool disableWorkerRunningAtProbe;
        private bool disableEnabledAfterProbe;
        private string disableProbeLeaf;
        private string disableHeldLeaf;
        private int disableProbeWorkerId = -1;
        private int disableEntriesBefore = -1;
        private int disableEntriesAtProbe = -1;
        private SavedNativeActor disableRiderDebt;
        private SavedNativeActor disableMountDebt;
        private string disableSavePath;

        private void AdvanceDisable()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P07 disable/re-enable timed out at " + disableStage +
                    ": " + persistence.Feedback);
            var game = Game.Instance;
            if (game == null) return;
            stage = 900 + disableStage;
            if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            if (disableStage == 0)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                Check(relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                    "P07-disable-opens-with-one-real-mounted-save");
                rider = relationship.Rider; mount = relationship.Mount;
                disableRiderId = rider.UniqueId; disableMountId = mount.UniqueId;
                disableRiderDebt = MountedPersistenceService.CaptureActor(rider);
                disableMountDebt = MountedPersistenceService.CaptureActor(mount);
                beforeControls = controls.CaptureSnapshot();
                disableFactsMounted = beforeControls.ExactFactCount;
                disableSlotsMounted = beforeControls.ManagedHotbarSlotCount;
                disableCastsBefore = controls.NativeCastRequestCount;
                Check(disableFactsMounted > 0 && beforeControls.DuplicateFactCount == 0,
                    "P07-disable-baseline-has-exact-owned-controls");
                Write("disable-initial", DisableDetail());
                disableFrames = 0; disableStage = 1;
                return;
            }
            if (disableStage == 1)
            {
                // Disabling outside a live operation must clean up fully, leaving
                // the native actors themselves usable.
                var result = relationship.Dismount(CleanupTrigger.ModDisabled);
                Check(result.Succeeded && relationship.State == RelationshipState.Unmounted,
                    "P07-disable-cleans-up-at-a-safe-boundary");
                var after = controls.CaptureSnapshot();
                disableFactsUnmounted = after.ExactFactCount;
                Check(after.DuplicateFactCount == 0 && !after.SerializationSuspended &&
                    after.ExactFactCount < disableFactsMounted,
                    "P07-disable-releases-owned-controls-without-residue");
                // The native actors survive cleanup and keep their expenditure.
                Check(game.State.Units.Count(u => u.UniqueId == disableRiderId) == 1 &&
                    game.State.Units.Count(u => u.UniqueId == disableMountId) == 1 &&
                    LegitimateContinuation(disableRiderDebt, MountedPersistenceService.CaptureActor(
                        game.State.Units.Single(u => u.UniqueId == disableRiderId)), 0),
                    "P07-disable-keeps-native-actors-usable-and-conserves-debt");
                // Idempotent: a second cleanup must not double-release.
                var again = relationship.Dismount(CleanupTrigger.ModDisabled);
                Check(relationship.State == RelationshipState.Unmounted &&
                    controls.CaptureSnapshot().ExactFactCount == disableFactsUnmounted &&
                    controls.CaptureSnapshot().DuplicateFactCount == 0,
                    "P07-repeated-disable-cleanup-is-idempotent");
                Write("disable-cleaned", DisableDetail());
                disableFrames = 0; disableStage = 2;
                return;
            }
            if (disableStage == 2)
            {
                if (++disableFrames < 6) return;
                // Re-enabling must rebuild exactly one pair and exactly one set of
                // owned controls, with no fresh Mount cast or duplicate grant.
                var remount = relationship.MountAutomationPair();
                Check(remount.Succeeded && relationship.State == RelationshipState.Mounted,
                    "P07-re-enable-restores-the-supported-pair");
                rider = relationship.Rider; mount = relationship.Mount;
                Check(rider.UniqueId == disableRiderId && mount.UniqueId == disableMountId,
                    "P07-re-enable-uses-the-same-native-actors");
                var after = controls.CaptureSnapshot();
                Check(after.ExactFactCount == disableFactsMounted && after.DuplicateFactCount == 0 &&
                    after.ManagedHotbarSlotCount == disableSlotsMounted,
                    "P07-re-enable-creates-no-duplicate-controls-or-slots");
                Check(controls.NativeCastRequestCount == disableCastsBefore,
                    "P07-re-enable-grants-nothing-through-a-fresh-Mount-cast");
                Check(relationship.Runtime.ValidateMountedInvariants() == null,
                    "P07-re-enabled-pair-holds-its-mounted-invariants");
                disableSecondMountRejected = !relationship.MountAutomationPair().Succeeded &&
                    relationship.State == RelationshipState.Mounted;
                Check(disableSecondMountRejected, "P07-re-enable-leaves-exactly-one-pair");
                Write("disable-re-enabled", DisableDetail());
                disableFrames = 0; disableStage = 3;
                return;
            }
            if (disableStage == 3)
            {
                // Clean up again, then save while unmounted: a later restore must
                // not resurrect the relationship or a stale actor.
                Check(relationship.Dismount(CleanupTrigger.ModDisabled).Succeeded &&
                    relationship.State == RelationshipState.Unmounted,
                    "P07-second-disable-cleans-up-again");
                callback = false;
                var target = game.SaveManager.FirstOrDefault(s => s.Name == "KMC_P01" && s.HasFileOnDisk)
                    ?? game.SaveManager.First(s => s.Name == "KMC_P01");
                disableSavePath = target.FolderName;
                disableEntriesBefore = NativeSaveWorkerBoundary.WorkerEntryCount;
                // Hold this one owned worker at its entry so the disable probe
                // below runs while the save is provably still able to commit,
                // instead of racing a write that may already have finished.
                NativeSaveWorkerBoundary.ArmWorkerHold(8000);
                game.SaveGame(target, () => callback = true);
                disableFrames = 0; disableStage = 4;
                return;
            }
            if (disableStage == 4)
            {
                if (!NativeSaveWorkerBoundary.WorkerHeld || !persistence.ActiveSaveWorkerRunning)
                {
                    if (++disableFrames > 600)
                        throw new InvalidOperationException(
                            "P07 disable probe never observed its own held owned save worker.");
                    return;
                }
                disableEntriesAtProbe = NativeSaveWorkerBoundary.WorkerEntryCount;
                disableHeldLeaf = NativeSaveWorkerBoundary.HeldWorkerLeaf;
                disableProbeLeaf = persistence.ActiveSaveLeaf;
                disableProbeWorkerId = persistence.ActiveSaveWorkerId;
                disableSuspendedAtProbe = persistence.SaveSuspended;
                disableWorkerRunningAtProbe = persistence.ActiveSaveWorkerRunning;
                Check(disableWorkerRunningAtProbe && disableEntriesAtProbe == disableEntriesBefore + 1 &&
                    !string.IsNullOrEmpty(disableHeldLeaf) && disableHeldLeaf == disableProbeLeaf &&
                    disableProbeWorkerId >= 0 && disableSuspendedAtProbe,
                    "P07-disable-probe-holds-this-exact-saves-own-running-worker");
                // The exact registered UMM toggle, not an internal shortcut.
                // Disabling here would run a full cleanup over the live graphs
                // this worker is serializing, so it must be refused.
                disableRefusedDuringSave = !Main.InvokeRegisteredToggleForAutomation(false);
                disableEnabledAfterProbe = persistence.Enabled;
                Check(disableRefusedDuringSave,
                    "P07-registered-disable-is-refused-while-an-owned-save-is-being-written");
                Check(disableEnabledAfterProbe && persistence.SaveSuspended &&
                    persistence.ActiveSaveWorkerRunning && persistence.FailedSaveCount == 0,
                    "P07-refused-disable-left-the-services-and-the-write-in-flight-untouched");
                Write("disable-refused-during-save", DisableDetail());
                NativeSaveWorkerBoundary.ReleaseWorkerHold();
                disableFrames = 0; disableStage = 5;
                return;
            }
            if (disableStage == 5)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                if (++disableFrames < 10) return;
                var archive = game.SaveManager.Where(s => s.Name == "KMC_P01" && s.HasFileOnDisk)
                    .OrderByDescending(s => new FileInfo(s.FolderName).LastWriteTimeUtc).First();
                var read = NativeMountedSaveStorage.Read(archive.Saver);
                // The cleanup-state save must record no pair at all, so a cold
                // load cannot rebuild one from it.
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted == false &&
                    read.Data.Rider == null && read.Data.Mount == null,
                    "P07-post-cleanup-save-records-no-mounted-pair");
                Check(relationship.State == RelationshipState.Unmounted &&
                    controls.CaptureSnapshot().DuplicateFactCount == 0,
                    "P07-post-cleanup-save-left-no-relationship-or-duplicate-control");
                disableSavePath = archive.FolderName;
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 2, ["path"] = archive.FolderName, ["sha256"] = Hash(archive.FolderName),
                    ["length"] = new FileInfo(archive.FolderName).Length, ["nativeType"] = archive.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = archive.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                Write("disable-saved-unmounted", DisableDetail());
                // Restore a usable mounted pair so the shared continuation and
                // teardown behave like every other P07 case.
                Check(relationship.MountAutomationPair().Succeeded &&
                    relationship.State == RelationshipState.Mounted,
                    "P07-pair-is-restorable-after-the-cleanup-state-save");
                rider = relationship.Rider; mount = relationship.Mount;
                recoveryContinuation = true; stage = 2;
            }
        }

        private JObject DisableDetail() => new JObject {
            ["case"] = request.PersistenceCase, ["stage"] = disableStage,
            ["relationship"] = relationship.State.ToString(),
            ["riderId"] = disableRiderId, ["mountId"] = disableMountId,
            ["factsMounted"] = disableFactsMounted, ["factsUnmounted"] = disableFactsUnmounted,
            ["slotsMounted"] = disableSlotsMounted,
            ["exactFactCount"] = SafeFacts(), ["duplicateFactCount"] = SafeDuplicates(),
            ["nativeCastRequests"] = controls.NativeCastRequestCount,
            ["castsBefore"] = disableCastsBefore,
            ["secondMountRejected"] = disableSecondMountRejected,
            ["refusedDuringSave"] = disableRefusedDuringSave,
            ["suspendedAtProbe"] = disableSuspendedAtProbe,
            ["workerRunningAtProbe"] = disableWorkerRunningAtProbe,
            ["enabledAfterProbe"] = disableEnabledAfterProbe,
            ["probeLeaf"] = disableProbeLeaf, ["heldLeaf"] = disableHeldLeaf,
            ["probeWorkerId"] = disableProbeWorkerId,
            ["workerEntriesBefore"] = disableEntriesBefore,
            ["workerEntriesAtProbe"] = disableEntriesAtProbe,
            ["teardownDrains"] = persistence.TeardownDrainCount,
            ["snapshots"] = persistence.SnapshotCount, ["failedSaves"] = persistence.FailedSaveCount,
            ["saveSuspended"] = persistence.SaveSuspended,
            ["savePath"] = disableSavePath, ["feedback"] = persistence.Feedback
        };

        private int? SafeFacts()
        {
            try { return controls.CaptureSnapshot().ExactFactCount; }
            catch (Exception exception) { logger.Exception("Disable fact observation unavailable", exception); return null; }
        }

        private int? SafeDuplicates()
        {
            try { return controls.CaptureSnapshot().DuplicateFactCount; }
            catch (Exception exception) { logger.Exception("Disable duplicate observation unavailable", exception); return null; }
        }
    }
}
