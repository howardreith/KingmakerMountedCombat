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
        private int disableFactsReEnabled = -1;
        private int disableSlotsReEnabled = -1;
        private int disableFactsEnabledUnmounted = -1;
        private int disableSlotsEnabledUnmounted = -1;
        private string disableInvariants;
        private bool disableSecondDisable;
        private bool disableSecondReEnable;
        private string disableStateAfterSecondDisable;
        private string disableStateAfterSecondReEnable;
        private int disableFactsAfterSecondDisable = -1;
        private int disableFactsAfterSecondReEnable = -1;
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
                // The exact registered UMM toggle, not an internal dismount: the
                // relationship cleanup and the owned control removal are separate
                // steps and only the real disable path runs both.
                Check(Main.InvokeRegisteredToggleForAutomation(false),
                    "P07-registered-disable-succeeds-at-a-safe-boundary");
                Check(relationship.State == RelationshipState.Unmounted,
                    "P07-disable-ends-the-mounted-relationship");
                var after = controls.CaptureSnapshot();
                disableFactsUnmounted = after.ExactFactCount;
                Check(after.ExactFactCount < disableFactsMounted,
                    "P07-disable-releases-owned-control-facts");
                Check(after.DuplicateFactCount == 0, "P07-disable-leaves-no-duplicated-control");
                Check(!after.SerializationSuspended, "P07-disable-leaves-no-serialization-lease");
                // The native actors survive cleanup and keep their expenditure.
                Check(game.State.Units.Count(u => u.UniqueId == disableRiderId) == 1 &&
                    game.State.Units.Count(u => u.UniqueId == disableMountId) == 1,
                    "P07-disable-keeps-both-native-actors-alive-exactly-once");
                Check(LegitimateContinuation(disableRiderDebt, MountedPersistenceService.CaptureActor(
                        game.State.Units.Single(u => u.UniqueId == disableRiderId)), 0),
                    "P07-disable-conserves-the-riders-native-expenditure");
                // Idempotent: a second disable must not double-release.
                Check(Main.InvokeRegisteredToggleForAutomation(false),
                    "P07-repeated-registered-disable-still-succeeds");
                var twice = controls.CaptureSnapshot();
                Check(relationship.State == RelationshipState.Unmounted &&
                    twice.ExactFactCount == disableFactsUnmounted && twice.DuplicateFactCount == 0,
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
                // Every action first, then the record, then the assertions: a
                // failed assertion must not destroy the measurement that would
                // explain it.
                var reEnabled = Main.InvokeRegisteredToggleForAutomation(true);
                var pairBeforeMount = relationship.State;
                // Enabling the services grants the unmounted-state control on its
                // own, with no pair. That count is the reference for every later
                // enabled-but-unmounted observation; comparing such a state to the
                // DISABLED count would demand the services grant nothing at all.
                var enabledIdle = controls.CaptureSnapshot();
                disableFactsEnabledUnmounted = enabledIdle.ExactFactCount;
                disableSlotsEnabledUnmounted = enabledIdle.ManagedHotbarSlotCount;
                var remount = relationship.MountAutomationPair();
                rider = relationship.Rider; mount = relationship.Mount;
                var after = controls.CaptureSnapshot();
                disableFactsReEnabled = after.ExactFactCount;
                disableSlotsReEnabled = after.ManagedHotbarSlotCount;
                disableInvariants = relationship.Runtime.ValidateMountedInvariants();
                disableSecondMountRejected = !relationship.MountAutomationPair().Succeeded &&
                    relationship.State == RelationshipState.Mounted;
                Write("disable-re-enabled", DisableDetail());
                Check(reEnabled, "P07-registered-re-enable-succeeds");
                Check(pairBeforeMount == RelationshipState.Unmounted,
                    "P07-re-enable-alone-invents-no-pair");
                Check(remount.Succeeded && relationship.State == RelationshipState.Mounted,
                    "P07-re-enable-restores-the-supported-pair");
                Check(rider.UniqueId == disableRiderId && mount.UniqueId == disableMountId,
                    "P07-re-enable-uses-the-same-native-actors");
                // Bounded, not equal: the requirement is that re-enabling grants
                // nothing extra and nothing twice. A save-restored baseline also
                // reinstates persisted state that a fresh pair has no reason to
                // recreate, so equality would assert something never established.
                // The exact re-enabled counts are recorded either way.
                Check(disableFactsEnabledUnmounted > disableFactsUnmounted &&
                    enabledIdle.DuplicateFactCount == 0,
                    "P07-re-enable-returns-the-unmounted-state-control-exactly-once");
                Check(disableFactsReEnabled >= disableFactsEnabledUnmounted &&
                    disableFactsReEnabled <= disableFactsMounted,
                    "P07-re-enable-returns-owned-controls-without-granting-extra");
                Check(after.DuplicateFactCount == 0 && disableSlotsReEnabled <= disableSlotsMounted,
                    "P07-re-enable-creates-no-duplicate-controls-or-slots");
                Check(controls.NativeCastRequestCount == disableCastsBefore,
                    "P07-re-enable-grants-nothing-through-a-fresh-Mount-cast");
                Check(disableInvariants == null, "P07-re-enabled-pair-holds-its-mounted-invariants");
                Check(disableSecondMountRejected, "P07-re-enable-leaves-exactly-one-pair");
                disableFrames = 0; disableStage = 3;
                return;
            }
            if (disableStage == 3)
            {
                // The refusal probe runs against a MOUNTED save: that is the case
                // whose live graphs a disable would mutate, and the owned save
                // scope that holds the serialization lease only exists for a pair.
                callback = false;
                var held = game.SaveManager.FirstOrDefault(s => s.Name == "KMC_P01" && s.HasFileOnDisk)
                    ?? game.SaveManager.First(s => s.Name == "KMC_P01");
                disableEntriesBefore = NativeSaveWorkerBoundary.WorkerEntryCount;
                // Hold this one owned worker at its entry so the probe runs while
                // the save is provably still able to commit, instead of racing a
                // write that may already have finished.
                NativeSaveWorkerBoundary.ArmWorkerHold(8000);
                game.SaveGame(held, () => callback = true);
                disableFrames = 0; disableStage = 4;
                return;
            }
            if (disableStage == 5)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                if (++disableFrames < 10) return;
                // Clean up again through the real toggle, restore the services,
                // then save while unmounted: a later restore must not resurrect
                // the relationship or a stale actor. Measure, record, then assert.
                disableSecondDisable = Main.InvokeRegisteredToggleForAutomation(false);
                disableStateAfterSecondDisable = relationship.State.ToString();
                disableFactsAfterSecondDisable = controls.CaptureSnapshot().ExactFactCount;
                disableSecondReEnable = Main.InvokeRegisteredToggleForAutomation(true);
                disableStateAfterSecondReEnable = relationship.State.ToString();
                var restored = controls.CaptureSnapshot();
                disableFactsAfterSecondReEnable = restored.ExactFactCount;
                Write("disable-cleared", DisableDetail());
                Check(disableSecondDisable, "P07-second-registered-disable-succeeds");
                Check(disableStateAfterSecondDisable == "Unmounted",
                    "P07-second-disable-ends-the-relationship-again");
                Check(disableFactsAfterSecondDisable == disableFactsUnmounted,
                    "P07-second-disable-releases-the-owned-controls-again");
                Check(disableSecondReEnable, "P07-services-return-after-the-second-disable");
                Check(disableStateAfterSecondReEnable == "Unmounted",
                    "P07-returning-services-resurrect-no-pair");
                // Cycle-stable, not zero: an enabled mod legitimately offers its
                // unmounted-state control. What must never happen is that count
                // growing, or duplicating, with each disable/re-enable cycle.
                Check(disableFactsAfterSecondReEnable == disableFactsEnabledUnmounted &&
                    restored.DuplicateFactCount == 0 &&
                    restored.ManagedHotbarSlotCount == disableSlotsEnabledUnmounted,
                    "P07-repeated-cycles-leave-the-enabled-unmounted-controls-unchanged");
                // Re-resolved from the manager's live enumeration: the previous
                // commit replaces its target in place and rebinds the path, so a
                // descriptor captured earlier is stale.
                callback = false;
                var target = game.SaveManager.Where(s => s.Name == "KMC_P01" && s.HasFileOnDisk)
                    .OrderByDescending(s => new FileInfo(s.FolderName).LastWriteTimeUtc).First();
                disableSavePath = target.FolderName;
                game.SaveGame(target, () => callback = true);
                disableFrames = 0; disableStage = 6;
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
            if (disableStage == 6)
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
            ["factsReEnabled"] = disableFactsReEnabled, ["slotsReEnabled"] = disableSlotsReEnabled,
            ["factsEnabledUnmounted"] = disableFactsEnabledUnmounted,
            ["slotsEnabledUnmounted"] = disableSlotsEnabledUnmounted,
            ["invariants"] = disableInvariants,
            ["secondDisable"] = disableSecondDisable, ["secondReEnable"] = disableSecondReEnable,
            ["stateAfterSecondDisable"] = disableStateAfterSecondDisable,
            ["stateAfterSecondReEnable"] = disableStateAfterSecondReEnable,
            ["factsAfterSecondDisable"] = disableFactsAfterSecondDisable,
            ["factsAfterSecondReEnable"] = disableFactsAfterSecondReEnable,
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
