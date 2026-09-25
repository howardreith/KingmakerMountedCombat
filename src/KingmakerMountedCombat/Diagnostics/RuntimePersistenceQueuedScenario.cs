using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool QueuedCase => SlotCase && request.PersistenceCase == "queued";
        private static readonly string[] QueuedNames = { "KMC_P05_QUEUE_1", "KMC_P05_QUEUE_2", "KMC_P01" };
        private readonly List<int> queuedCallbacks = new List<int>();
        private string queuedObservationError;

        private void QueueNativeManualSaves()
        {
            var game = Game.Instance;
            Check(!Cold && persistence.SnapshotCount == 0 && !NativePersistenceIsolation.HasPendingWrites &&
                game.SaveManager.IsSaveAllowed(), "P05-native-queued-request-admission");
            // Native SaveGame/LoadingProcess own the three records and their order.
            // No IEnumerator is driven by the fixture and no snapshot is injected.
            for (var i = 0; i < QueuedNames.Length; i++)
            {
                var ordinal = i + 1;
                var descriptor = game.SaveManager.CreateNewSave(QueuedNames[i]);
                Check(descriptor.Type == SaveInfo.SaveType.Manual && descriptor.Name == QueuedNames[i] &&
                    !descriptor.IsActuallySaved, "P05-unique-new-native-queued-descriptor");
                game.SaveGame(descriptor, () =>
                {
                    // A diagnostic observation must not escape into native loading
                    // and turn an assertion failure into a world-load exception.
                    try
                    {
                        queuedCallbacks.Add(ordinal);
                        Write("queued-native-callback", new JObject {
                            ["ordinal"] = ordinal, ["snapshots"] = persistence.SnapshotCount,
                            ["pendingWrites"] = NativePersistenceIsolation.HasPendingWrites });
                    }
                    catch (Exception error) { queuedObservationError = error.Message; }
                });
            }
            Check(persistence.SnapshotCount == 0 && queuedCallbacks.Count == 0,
                "P05-queueing-is-not-the-snapshot-or-write-boundary");
            Write("queued-native-requests", new JObject {
                ["count"] = QueuedNames.Length, ["names"] = new JArray(QueuedNames),
                ["snapshots"] = persistence.SnapshotCount, ["callbacks"] = queuedCallbacks.Count });
        }

        private bool ObserveQueuedWrites()
        {
            if (queuedObservationError != null) throw new InvalidOperationException(queuedObservationError);
            if (queuedCallbacks.Count < QueuedNames.Length || NativePersistenceIsolation.HasPendingWrites) return false;
            var game = Game.Instance;
            Check(queuedCallbacks.SequenceEqual(new[] { 1, 2, 3 }) && persistence.SnapshotCount == 3,
                "P05-native-queue-enumerates-each-snapshot-and-callback-once");
            var after = controls.CaptureSnapshot();
            Check(relationship.State == RelationshipState.Mounted && relationship.Rider == rider &&
                relationship.Mount == mount && after.ExactFactCount == beforeControls.ExactFactCount &&
                after.DuplicateFactCount == 0 && after.ManagedHotbarSlotCount == beforeControls.ManagedHotbarSlotCount &&
                !after.SerializationSuspended && !game.IsPaused && game.CurrentMode == GameModeType.Default,
                "P05-entire-native-queue-restores-controls-and-live-play");
            for (var i = 0; i < QueuedNames.Length; i++)
            {
                var save = game.SaveManager.SingleOrDefault(s => s.Name == QueuedNames[i]);
                Check(save != null && save.Type == SaveInfo.SaveType.Manual &&
                    save.OperationState == SaveInfo.StateType.None && save.HasFileOnDisk &&
                    save.FileName == "Manual_" + (300 + i) + "_" + QueuedNames[i] + ".zks",
                    "P05-each-queued-native-archive-actually-committed");
                var read = NativeMountedSaveStorage.Read(save.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId &&
                    read.Data.Slots.Length == beforeControls.ManagedHotbarSlotCount,
                    "P05-each-queued-archive-has-its-own-native-pair-metadata");
                var elapsed = Math.Max(0, (game.TimeController.GameTime.Ticks - read.Data.GameTimeTicks) /
                    (double)TimeSpan.TicksPerSecond);
                Check(LegitimateContinuation(read.Data.Rider, MountedPersistenceService.CaptureActor(rider), elapsed) &&
                    LegitimateContinuation(read.Data.Mount, MountedPersistenceService.CaptureActor(mount), elapsed),
                    "P05-queued-saving-does-not-tax-or-refund-native-work");
                Write(i < 2 ? "queued-native-write-complete" : "native-write-complete", new JObject {
                    ["ordinal"] = i + 1, ["path"] = save.FolderName, ["sha256"] = Hash(save.FolderName),
                    ["length"] = new FileInfo(save.FolderName).Length, ["nativeType"] = save.Type.ToString(),
                    ["nativeCallback"] = queuedCallbacks.Contains(i + 1), ["operation"] = save.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
            }
            Check(Directory.GetFiles(game.SaveManager.SavePath, "*.zks").Length == 4,
                "P05-native-queue-retains-three-writes-and-read-only-fixture-only");
            return true;
        }
    }
}
