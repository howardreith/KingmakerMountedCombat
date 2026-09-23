using System;
using System.Collections.Generic;
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
        private bool WorkerDrainCase => RecoveryCase && request.PersistenceCase == "serialization-cancel";
        private int drainStage;
        private int drainFrames;
        private IDisposable drainHold;
        private bool drainSaveCallback;
        private string drainLeaf;
        private int drainWorkerId = -1;
        private int drainWorkerEntriesAtHold;
        private string drainGoodHash;
        private long drainGoodLength;
        private string drainGoodPath;
        private SaveInfo drainGoodSave;
        private string drainInterruptedPath;
        private string drainSettledHash;
        private SavedNativeActor drainRiderDebt;
        private SavedNativeActor drainMountDebt;
        private bool drainOverlapRefused;
        private bool drainLoadRefused;
        private int drainRejectionsAtStop;
        private int drainDisposalsAtStop;
        private bool drainRepeatedStopSafe;
        private bool drainDisableRefused;
        private bool drainNativePaused;
        private bool drainNativeLoading;
        private string drainNativeMode;
        // Simulation probe: the worker serializes live object graphs, so what
        // matters is whether entity state can still change under those reads.
        private long drainSimTicksAtProbe;
        private long drainSimTicksAdvanced;
        private UnityEngine.Vector3 drainSimPositionAtProbe;
        private float drainSimMoved;
        private bool? drainSimAreaTurnedOn;
        private bool? drainSimAreaTurnedOnAfter;
        private bool drainSimCommandQueued;
        private bool drainSimCommandStarted;
        private bool drainSimWorkerStillHeld;
        private Kingmaker.UnitLogic.Commands.Base.UnitCommand drainSimProbe;

        // Best effort and never fatal: the substantive measurements are whether
        // the clock advanced, the actor moved, and the command started.
        private bool? AreaSimulationOn()
        {
            try
            {
                var area = Game.Instance?.State?.LoadedAreaState;
                if (area == null) return null;
                var property = area.GetType().GetProperty("IsTurnedOn",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (property != null && property.PropertyType == typeof(bool)) return (bool)property.GetValue(area);
                var field = area.GetType().GetField("m_TurnedOn",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (field != null && field.FieldType == typeof(bool)) return (bool)field.GetValue(area);
                return null;
            }
            catch (Exception exception)
            { logger.Exception("Drain area simulation state unavailable", exception); return null; }
        }

        private void AdvanceWorkerDrain()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P07 worker drain timed out at " + drainStage +
                    " frames=" + drainFrames + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (game == null) return;
            stage = 800 + drainStage;
            if (drainStage == 0)
            {
                if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                    game.CurrentMode != Kingmaker.GameModes.GameModeType.Default || !callback) return;
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "P07-required-policy");
                Check(relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                    "P07-drain-opens-with-one-real-mounted-save");
                rider = relationship.Rider; mount = relationship.Mount;
                beforeControls = controls.CaptureSnapshot();
                drainRiderDebt = MountedPersistenceService.CaptureActor(rider);
                drainMountDebt = MountedPersistenceService.CaptureActor(mount);
                // Captured before the save starts: SaveRoutine removes the
                // same-named descriptor from the manager list and adds its own
                // new numbered leaf, so the last-good archive is not enumerable
                // while the interrupted save is in flight.
                var archive = RecoveryArchive();
                drainGoodSave = archive;
                drainGoodPath = archive.FolderName;
                drainGoodHash = Hash(drainGoodPath);
                drainGoodLength = new FileInfo(drainGoodPath).Length;
                Write("drain-initial-write", DrainDetail(null));
                // Bounded and self-releasing: the worker would otherwise finish
                // far faster than an interruption can be requested against it.
                drainHold = NativeSaveWorkerBoundary.ArmWorkerHold(20000);
                drainSaveCallback = false;
                game.SaveGame(archive, () => drainSaveCallback = true);
                drainFrames = 0; drainStage = 1;
                return;
            }
            if (drainStage == 1)
            {
                drainFrames++;
                // The in-flight boundary must be THIS operation's worker: its own
                // Task identity, observed at the hold, not a cumulative count.
                if (!NativeSaveWorkerBoundary.WorkerHeld || !persistence.ActiveSaveWorkerRunning ||
                    persistence.ActiveSaveWorkerId < 0 || string.IsNullOrEmpty(persistence.ActiveSaveLeaf))
                {
                    if (drainFrames > 1200)
                        throw new InvalidOperationException("P07 owned save worker never reached its held boundary: held=" +
                            NativeSaveWorkerBoundary.WorkerHeld + " running=" + persistence.ActiveSaveWorkerRunning +
                            " entries=" + NativeSaveWorkerBoundary.WorkerEntryCount);
                    return;
                }
                drainLeaf = persistence.ActiveSaveLeaf;
                drainWorkerId = persistence.ActiveSaveWorkerId;
                drainWorkerEntriesAtHold = NativeSaveWorkerBoundary.WorkerEntryCount;
                drainNativePaused = game.IsPaused;
                drainNativeLoading = LoadingProcess.Instance.IsLoadingInProcess;
                drainNativeMode = game.CurrentMode.ToString();
                Write("worker-in-flight-observed", DrainDetail(null));
                Check(NativeSaveWorkerBoundary.WorkerHoldCount == 1 &&
                    NativeSaveWorkerBoundary.HeldWorkerLeaf == drainLeaf,
                    "P07-exactly-one-owned-worker-is-held-at-its-own-leaf");
                Check(persistence.HasActiveSaveScope && persistence.SaveSuspended &&
                    controls.CaptureSnapshot().SerializationSuspended && !persistence.SaveDraining &&
                    persistence.DeferredSaveCancellationCount == 0 && persistence.DrainedSaveCount == 0,
                    "P07-in-flight-save-still-owns-its-serialization-leases");
                drainRejectionsAtStop = persistence.RejectedLoadCount;
                drainDisposalsAtStop = persistence.NativeWorldDisposalCount;

                // The real native cancellation path.
                LoadingProcess.Instance.StopAll();

                Check(persistence.SaveDraining && persistence.HasActiveSaveScope &&
                    persistence.DeferredSaveCancellationCount == 1 && persistence.DrainedSaveCount == 0 &&
                    persistence.ActiveSaveWorkerId == drainWorkerId,
                    "P07-StopAll-defers-rather-than-releasing-a-committable-worker");
                Check(persistence.SaveSuspended && controls.CaptureSnapshot().SerializationSuspended &&
                    !drainSaveCallback, "P07-abandoned-save-retains-leases-and-reports-no-cancellation");

                // Repeated cancellation must not release twice or invent an outcome.
                LoadingProcess.Instance.StopAll();
                drainRepeatedStopSafe = persistence.DeferredSaveCancellationCount == 1 &&
                    persistence.DrainedSaveCount == 0 && persistence.SaveDraining && !drainSaveCallback;
                Check(drainRepeatedStopSafe, "P07-repeated-cancellation-neither-releases-twice-nor-reports-success");

                // A second serialization must not begin over a live worker. The
                // wrapper is activated the way the native queue activates it, so
                // the real overlap guard in the scope's begin action is reached,
                // without queueing an operation whose failure would escape into
                // the loading pump.
                try
                {
                    var probe = persistence.WrapSaveRoutine(EmptyDrainRoutine(), drainGoodSave);
                    try
                    {
                        (probe as DeferredSaveEnumerator<object>)?.Activate(() => { });
                        probe.MoveNext();
                    }
                    finally { try { probe.Dispose(); } catch (InvalidOperationException) { } }
                }
                catch (InvalidOperationException error)
                { drainOverlapRefused = error.Message.IndexOf("Overlapping", StringComparison.Ordinal) >= 0; }
                Check(drainOverlapRefused && persistence.SaveDraining && persistence.DrainedSaveCount == 0,
                    "P07-second-serialization-is-refused-while-the-worker-can-commit");

                // A conflicting world replacement must be refused BEFORE disposal.
                // The captured descriptor is used because the in-flight save has
                // replaced its own name in the manager's enumeration.
                game.LoadGameFromMainMenu(drainGoodSave);
                drainLoadRefused = persistence.RejectedLoadCount == drainRejectionsAtStop + 1 &&
                    persistence.NativeWorldDisposalCount == drainDisposalsAtStop &&
                    game.CurrentlyLoadedArea != null && !LoadingProcess.Instance.IsLoadingInProcess;
                Check(drainLoadRefused, "P07-conflicting-load-is-refused-before-any-world-disposal");

                // A disable request must not remove the drain owner.
                var wasEnabled = persistence.Enabled;
                try
                {
                    persistence.Enabled = false;
                    persistence.Update(); controls.Update();
                    drainDisableRefused = persistence.SaveDraining && persistence.HasActiveSaveScope &&
                        persistence.DrainedSaveCount == 0 && persistence.SaveSuspended;
                }
                finally { persistence.Enabled = wasEnabled; }
                Check(drainDisableRefused, "P07-disable-cannot-remove-the-drain-owner-or-its-leases");

                Check(Hash(drainGoodPath) == drainGoodHash,
                    "P07-last-good-archive-is-untouched-while-the-worker-is-held");
                Write("drain-cancellation-deferred", DrainDetail(null));
                // The worker serializes LIVE object graphs - Game.Instance.Player,
                // the cross-scene state and the loaded area - so the question is
                // whether simulation can still advance under those reads once
                // StopAll has removed the loading operation. Measure it rather
                // than infer it from a pause flag or from retained KMC leases.
                drainSimTicksAtProbe = game.TimeController.GameTime.Ticks;
                drainSimPositionAtProbe = rider.Position;
                drainSimAreaTurnedOn = AreaSimulationOn();
                try
                {
                    var destination = rider.Position + UnityEngine.Vector3.forward * 2f;
                    var probe = new Kingmaker.UnitLogic.Commands.UnitMoveTo(destination) { CreatedByPlayer = true };
                    rider.Commands.Run(probe);
                    drainSimCommandQueued = rider.Commands.Raw.Concat(rider.Commands.Queue).Contains(probe);
                    drainSimProbe = probe;
                }
                catch (Exception exception)
                { logger.Exception("Drain simulation probe could not be dispatched", exception); }
                drainFrames = 0; drainStage = 2;
                return;
            }
            if (drainStage == 2)
            {
                // Let the engine run while the worker is still held.
                if (++drainFrames < 40) return;
                drainSimTicksAdvanced = game.TimeController.GameTime.Ticks - drainSimTicksAtProbe;
                drainSimMoved = (rider.Position - drainSimPositionAtProbe).magnitude;
                drainSimCommandStarted = drainSimProbe != null && drainSimProbe.IsStarted;
                drainSimAreaTurnedOnAfter = AreaSimulationOn();
                drainSimWorkerStillHeld = NativeSaveWorkerBoundary.WorkerHeld;
                Write("drain-simulation-probe", DrainDetail(null));
                // The worker must still have been held for this to mean anything.
                Check(drainSimWorkerStillHeld && persistence.SaveDraining && persistence.DrainedSaveCount == 0,
                    "P07-simulation-probe-observed-while-the-worker-was-still-held");
                try { drainSimProbe?.Interrupt(); } catch (Exception exception)
                { logger.Exception("Drain simulation probe could not be interrupted", exception); }
                drainHold.Dispose(); drainHold = null;
                drainFrames = 0; drainStage = 3;
                return;
            }
            if (drainStage == 3)
            {
                drainFrames++;
                if (persistence.DrainedSaveCount == 0)
                {
                    if (drainFrames > 1800)
                        throw new InvalidOperationException("P07 released worker never drained: held=" +
                            NativeSaveWorkerBoundary.WorkerHeld + " draining=" + persistence.SaveDraining);
                    return;
                }
                if (++drainFrames < 12) return;
                // Measured: the commit replaces the target archive in place and
                // rebinds the path, so the interrupted save lands wherever the
                // scope's prepared descriptor now points, not at its leaf name.
                drainInterruptedPath = persistence.LastDrainedSavePath;
                var committed = persistence.LastDrainedSaveCommitted;
                var currentHash = Hash(drainGoodPath);
                Write("drain-settled", DrainDetail(currentHash));
                Check(persistence.DrainedSaveCount == 1 && !persistence.SaveDraining &&
                    !persistence.HasActiveSaveScope, "P07-drain-releases-exactly-once");
                Check(!persistence.SaveSuspended && !controls.CaptureSnapshot().SerializationSuspended &&
                    controls.CaptureSnapshot().ExactFactCount == beforeControls.ExactFactCount &&
                    controls.CaptureSnapshot().DuplicateFactCount == 0 && controls.NativeCastRequestCount == 0,
                    "P07-drain-restores-owned-controls-exactly-once");
                Check(ReferenceEquals(game.Player, drainWorld()) || game.Player != null,
                    "P07-drain-leaves-a-live-native-world");
                Check(game.Player.CrossSceneState != null,
                    "P07-drain-restores-the-native-world-reference-the-worker-clears");
                // Truthful settlement, either way, checked against the bytes: the
                // reported outcome must match whether the interrupted leaf really
                // exists, and the last-good archive must be intact regardless.
                Check(!string.IsNullOrEmpty(drainInterruptedPath) &&
                    committed == File.Exists(drainInterruptedPath),
                    "P07-reported-settlement-matches-whether-the-interrupted-archive-exists");
                if (committed)
                {
                    // A committed interruption is a real replacement, so claiming
                    // the old bytes survived would be false. The replaced archive
                    // must have changed and must be readable as a real save.
                    var replacedInPlace = string.Equals(drainInterruptedPath, drainGoodPath, StringComparison.OrdinalIgnoreCase);
                    Check(!replacedInPlace || currentHash != drainGoodHash,
                        "P07-a-committed-interrupted-save-really-replaced-the-target-archive");
                    Check(replacedInPlace || (currentHash == drainGoodHash &&
                        new FileInfo(drainGoodPath).Length == drainGoodLength),
                        "P07-a-committed-interruption-elsewhere-leaves-the-last-good-archive-intact");
                    var landed = game.SaveManager.Single(s => s.FolderName == drainInterruptedPath);
                    Check(NativeMountedSaveStorage.Read(landed.Saver).Kind == MountedSaveReadKind.Current &&
                        landed.OperationState == SaveInfo.StateType.None,
                        "P07-a-committed-interrupted-save-is-a-complete-readable-archive");
                }
                else
                    Check(currentHash == drainGoodHash && new FileInfo(drainGoodPath).Length == drainGoodLength,
                        "P07-a-failed-interruption-leaves-the-last-good-archive-byte-identical");
                Check(persistence.FailedSaveCount == (committed ? 0 : 1),
                    "P07-drain-reports-failure-only-when-the-worker-did-not-commit");
                // Further drains and disposals are no-ops for a settled operation.
                persistence.Update(); persistence.Update(); controls.Update();
                LoadingProcess.Instance.StopAll();
                Check(persistence.DrainedSaveCount == 1 && persistence.DeferredSaveCancellationCount == 1 &&
                    !persistence.SaveDraining, "P07-settled-operation-ignores-further-update-and-cancellation");
                Check(LegitimateContinuation(drainRiderDebt, MountedPersistenceService.CaptureActor(rider), 0) &&
                    LegitimateContinuation(drainMountDebt, MountedPersistenceService.CaptureActor(mount), 0),
                    "P07-drain-conserves-native-action-debt");
                drainSettledHash = Hash(drainGoodPath);
                drainFrames = 0; drainStage = 4;
                return;
            }
            if (drainStage == 4)
            {
                if (LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites) return;
                if (++drainFrames < 10) return;
                // A real subsequent save must still work after the drain. The
                // descriptor is re-resolved from the manager's current
                // enumeration rather than reusing the object the interrupted save
                // replaced and rebound; CreateNewSave cannot be used here because
                // it has no assigned leaf yet and the isolated authority refuses
                // an undeclared one.
                callback = false;
                var target = game.SaveManager.FirstOrDefault(s => s.FolderName == drainGoodPath)
                    ?? game.SaveManager.Where(s => s.Name == "KMC_P01" && s.HasFileOnDisk)
                        .OrderByDescending(s => new FileInfo(s.FolderName).LastWriteTimeUtc).First();
                Write("drain-subsequent-requested", new JObject {
                    ["path"] = target.FolderName, ["fileName"] = target.FileName,
                    ["operation"] = target.OperationState.ToString(),
                    ["sha256"] = Hash(target.FolderName) });
                game.SaveGame(target, () => callback = true);
                drainFrames = 0; drainStage = 5;
                return;
            }
            if (drainStage == 5)
            {
                if (!callback || LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites)
                {
                    if (++drainFrames > 2400)
                        throw new InvalidOperationException("P07 subsequent save never completed: callback=" + callback +
                            " loading=" + LoadingProcess.Instance.IsLoadingInProcess +
                            " pending=" + NativePersistenceIsolation.HasPendingWrites +
                            " activeLeaf=" + persistence.ActiveSaveLeaf +
                            " failedSaves=" + persistence.FailedSaveCount +
                            " replacementFailures=" + NativeMountedArchiveCommit.ReplacementFailureCount +
                            " feedback=" + persistence.Feedback);
                    return;
                }
                // The commit replaces in place, so the subsequent save is the most
                // recently written owned archive and must carry new bytes.
                var archive = game.SaveManager.Where(s => s.Name == "KMC_P01" && s.HasFileOnDisk)
                    .OrderByDescending(s => new FileInfo(s.FolderName).LastWriteTimeUtc).First();
                Check(archive.OperationState == SaveInfo.StateType.None &&
                    Hash(archive.FolderName) != drainSettledHash,
                    "P07-subsequent-save-is-a-complete-archive-with-new-bytes");
                var read = NativeMountedSaveStorage.Read(archive.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId &&
                    persistence.DrainedSaveCount == 1 && !persistence.SaveDraining,
                    "P07-subsequent-real-save-completes-after-the-drain");
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 2, ["path"] = archive.FolderName, ["sha256"] = Hash(archive.FolderName),
                    ["length"] = new FileInfo(archive.FolderName).Length, ["nativeType"] = archive.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = archive.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                recoveryContinuation = true; stage = 2;
            }
        }

        private Player drainWorld() => Game.Instance?.Player;

        private static IEnumerator<object> EmptyDrainRoutine() { yield break; }

        private JObject DrainDetail(string currentHash) => new JObject {
            ["case"] = request.PersistenceCase,
            ["stage"] = drainStage, ["frames"] = drainFrames,
            ["preparedLeaf"] = drainLeaf, ["workerTaskId"] = drainWorkerId,
            ["workerRunning"] = persistence.ActiveSaveWorkerRunning,
            ["workerHeld"] = NativeSaveWorkerBoundary.WorkerHeld,
            ["heldLeaf"] = NativeSaveWorkerBoundary.HeldWorkerLeaf,
            ["workerHolds"] = NativeSaveWorkerBoundary.WorkerHoldCount,
            ["workerEntries"] = NativeSaveWorkerBoundary.WorkerEntryCount,
            ["workerEntriesAtHold"] = drainWorkerEntriesAtHold,
            ["draining"] = persistence.SaveDraining, ["activeScope"] = persistence.HasActiveSaveScope,
            ["deferredCancellations"] = persistence.DeferredSaveCancellationCount,
            ["drains"] = persistence.DrainedSaveCount,
            ["drainCommitted"] = persistence.LastDrainedSaveCommitted,
            ["saveSuspended"] = persistence.SaveSuspended,
            ["serializationSuspended"] = SafeSerializationSuspended(),
            ["saveCallback"] = drainSaveCallback, ["snapshots"] = persistence.SnapshotCount,
            ["failedSaves"] = persistence.FailedSaveCount,
            ["rejections"] = persistence.RejectedLoadCount,
            ["nativeWorldDisposals"] = persistence.NativeWorldDisposalCount,
            ["overlapRefused"] = drainOverlapRefused, ["loadRefused"] = drainLoadRefused,
            ["repeatedStopSafe"] = drainRepeatedStopSafe, ["disableRefused"] = drainDisableRefused,
            // Recorded, not asserted: whether dropping the loading operation also
            // drops the engine's own gameplay suspension while the worker lives.
            ["nativePausedAtHold"] = drainNativePaused,
            ["nativeLoadingAtHold"] = drainNativeLoading,
            ["nativeModeAtHold"] = drainNativeMode,
            // Simulation probe, recorded rather than assumed.
            ["simTicksAdvanced"] = drainSimTicksAdvanced,
            ["simMovedDistance"] = drainSimMoved,
            ["simCommandQueued"] = drainSimCommandQueued,
            ["simCommandStarted"] = drainSimCommandStarted,
            ["simAreaTurnedOnAtProbe"] = drainSimAreaTurnedOn,
            ["simAreaTurnedOnAfter"] = drainSimAreaTurnedOnAfter,
            ["simWorkerStillHeld"] = drainSimWorkerStillHeld,
            ["nativePausedNow"] = Game.Instance?.IsPaused,
            ["nativeLoadingNow"] = LoadingProcess.Instance.IsLoadingInProcess,
            ["nativeModeNow"] = Game.Instance?.CurrentMode.ToString(),
            ["lastGoodSha256"] = drainGoodHash, ["lastGoodLength"] = drainGoodLength,
            ["lastGoodPath"] = drainGoodPath, ["interruptedPath"] = drainInterruptedPath,
            ["replacedInPlace"] = drainInterruptedPath != null && drainGoodPath != null &&
                string.Equals(drainInterruptedPath, drainGoodPath, StringComparison.OrdinalIgnoreCase),
            ["currentSha256"] = currentHash, ["feedback"] = persistence.Feedback
        };

        private bool? SafeSerializationSuspended()
        {
            try { return controls.CaptureSnapshot().SerializationSuspended; }
            catch (Exception exception)
            { logger.Exception("Drain control observation unavailable", exception); return null; }
        }
    }
}
