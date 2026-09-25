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
        private bool AlternatingCase => SlotCase && request.PersistenceCase == "alternating";
        private bool alternatingContinuation;
        private int alternatingStage;
        private int alternatingLoadIndex;
        private int alternatingReadyFrames;
        private string alternatingRiderId;
        private string alternatingMountId;
        private UnitEntityData previousAlternatingRider;
        private UnitEntityData previousAlternatingMount;
        private bool alternatingLoadCallback;

        private void AdvanceAlternating()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P05 alternating boundary timed out: " + alternatingStage);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            stage = 200 + alternatingStage;
            if (alternatingStage == 0)
            {
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "alternating-required-policy");
                if (Cold)
                {
                    Check(persistence.LoadedData?.Mounted == true && relationship.State == RelationshipState.Mounted,
                        "initial-A-restored-from-actual-archive");
                    alternatingRiderId = persistence.LoadedData.Rider.Id;
                    alternatingMountId = persistence.LoadedData.Mount.Id;
                    ValidateAlternatingWorld(true, 2, 1);
                    beforeControls = controls.CaptureSnapshot();
                    Write("initial");
                    Write("alternate-world-loaded", AlternatingDetail("A", 0));
                    RequestAlternatingLoad(request.PersistenceAlternate, 1);
                    alternatingStage = 20;
                    return;
                }
                string error;
                if (!relationship.TryResolveAutomationPair(out rider, out mount, out error))
                    throw new InvalidOperationException(error);
                Check(!game.Player.IsInCombat && relationship.MountRiderOn(rider, mount).Succeeded, "alternating-source-mount");
                controls.Update(); BindOwnedControlSlots();
                alternatingRiderId = rider.UniqueId; alternatingMountId = mount.UniqueId;
                beforeControls = controls.CaptureSnapshot();
                Write("initial");
                RequestAlternatingWrite("KMC_P01");
                alternatingStage = 1;
                return;
            }
            if (alternatingStage == 1)
            {
                if (!ObserveAlternatingWrite("KMC_P01", "Manual_300_KMC_P01.zks", true, 1, "A")) return;
                Check(relationship.Dismount(CleanupTrigger.Manual).Succeeded && !game.Player.IsInCombat,
                    "source-voluntary-dismount");
                controls.Update();
                Write("alternate-voluntary-dismount");
                alternatingReadyFrames = 0; alternatingStage = 2;
                return;
            }
            if (alternatingStage == 2)
            {
                if (!rider.Commands.Empty || !mount.Commands.Empty) { alternatingReadyFrames = 0; return; }
                if (++alternatingReadyFrames < 5) return;
                Check(relationship.State == RelationshipState.Unmounted && relationship.Rider == null && relationship.Mount == null,
                    "source-B-native-unmounted");
                RequestAlternatingWrite("KMC_P05_UNMOUNTED");
                alternatingStage = 3;
                return;
            }
            if (alternatingStage == 3)
            {
                if (!ObserveAlternatingWrite("KMC_P05_UNMOUNTED", "Manual_301_KMC_P05_UNMOUNTED.zks", false, 2, "B")) return;
                Check(game.SaveManager.Count() == 3, "source-keeps-A-B-and-read-only-fixture");
                Write("alternating-source-complete");
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
                return;
            }
            if (alternatingStage == 20)
            {
                if (!alternatingLoadCallback || ++alternatingReadyFrames < 10) return;
                var mounted = alternatingLoadIndex == 2;
                ValidateAlternatingWorld(mounted, mounted ? 4 : 2, mounted ? 2 : 1);
                Check(!ReferenceEquals(rider, previousAlternatingRider) && !ReferenceEquals(mount, previousAlternatingMount),
                    "new-native-world-objects-for-selected-archive");
                Write("alternate-world-loaded", AlternatingDetail(mounted ? "A" : "B", alternatingLoadIndex));
                if (!mounted) { RequestAlternatingLoad(request.PersistenceLoad, 2); return; }
                Check(Hash(Path.Combine(game.SaveManager.SavePath, request.PersistenceLoad.FileName)) == request.PersistenceLoad.Sha256 &&
                    Hash(Path.Combine(game.SaveManager.SavePath, request.PersistenceAlternate.FileName)) == request.PersistenceAlternate.Sha256,
                    "alternating-input-archives-unchanged");
                beforeControls = controls.CaptureSnapshot();
                RequestAlternatingWrite("KMC_P05_POST");
                alternatingStage = 21;
                return;
            }
            if (alternatingStage == 21)
            {
                if (!ObserveAlternatingWrite("KMC_P05_POST", "Manual_302_KMC_P05_POST.zks", true, 1, "post-cold")) return;
                Check(game.SaveManager.Count() == 3, "cold-inputs-and-new-native-save-present");
                Write("alternating-cycle-complete");
                alternatingContinuation = true;
                stage = 2; // Existing ordinary native movement/attack continuation.
            }
        }

        private void ValidateAlternatingWorld(bool mounted, int semanticCount, int presentationCount)
        {
            var game = Game.Instance;
            var data = persistence.LoadedData;
            Check(data != null && data.Mounted == mounted && data.Combat == null &&
                data.CampaignId == game.Player.GameId && data.AreaId == game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                "selected-archive-semantic-state");
            var riders = game.State.Units.Where(u => u.UniqueId == alternatingRiderId).ToArray();
            var mounts = game.State.Units.Where(u => u.UniqueId == alternatingMountId).ToArray();
            Check(riders.Length == 1 && mounts.Length == 1, "same-unique-native-actors-in-new-world");
            rider = riders[0]; mount = mounts[0];
            Check(!game.Player.IsInCombat && !persistence.SaveSuspended &&
                persistence.SemanticRestoreCount == semanticCount && persistence.PresentationRestoreCount == presentationCount,
                "no-stale-encounter-or-replayed-restoration");
            if (mounted)
                Check(relationship.State == RelationshipState.Mounted && relationship.Rider == rider &&
                    relationship.Mount == mount && data.Rider.Id == rider.UniqueId && data.Mount.Id == mount.UniqueId,
                    "A-restores-only-its-saved-pair");
            else
                Check(relationship.State == RelationshipState.Unmounted && relationship.Rider == null && relationship.Mount == null &&
                    data.Rider == null && data.Mount == null && data.ProfileId == null, "B-does-not-inherit-A-pair");
            controls.Update();
            var actual = controls.CapturePersistentSlots();
            Write("alternate-controls-observed", new JObject {
                ["mounted"] = mounted,
                ["saved"] = JArray.FromObject(data.Slots, MountedSaveCodec.CreateSerializer()),
                ["actual"] = JArray.FromObject(actual, MountedSaveCodec.CreateSerializer()) });
            Check(actual.Length == data.Slots.Length && data.Slots.All(s =>
                actual.Any(a => a.ActorId == s.ActorId && a.Index == s.Index && a.Kind == s.Kind)),
                "selected-archive-owned-control-bindings");
            Check(controls.CaptureSnapshot().DuplicateFactCount == 0 && controls.NativeCastRequestCount == 0,
                "no-duplicate-facts-or-replayed-mount");
        }

        private JObject AlternatingDetail(string label, int index) => new JObject {
            ["label"] = label, ["index"] = index, ["mounted"] = persistence.LoadedData.Mounted,
            ["semanticCount"] = persistence.SemanticRestoreCount, ["presentationCount"] = persistence.PresentationRestoreCount,
            ["freshNativeObjects"] = index == 0 || !ReferenceEquals(rider, previousAlternatingRider) && !ReferenceEquals(mount, previousAlternatingMount),
            ["slots"] = JArray.FromObject(persistence.LoadedData.Slots, MountedSaveCodec.CreateSerializer()) };

        private void RequestAlternatingLoad(RuntimeSaveDescriptor expected, int index)
        {
            var game = Game.Instance;
            var path = Path.Combine(game.SaveManager.SavePath, expected.FileName);
            Check(Hash(path) == expected.Sha256, "exact-next-owned-archive-hash");
            var descriptor = game.SaveManager.Single(s => s.FileName == expected.FileName);
            Check(descriptor.Name == expected.InternalName && descriptor.Type == SaveInfo.SaveType.Manual &&
                descriptor.GameId == expected.GameId && descriptor.GameName == expected.GameName &&
                descriptor.Area.AssetGuidThreadSafe == expected.Area, "exact-next-native-load-descriptor");
            previousAlternatingRider = rider; previousAlternatingMount = mount;
            alternatingLoadIndex = index; alternatingReadyFrames = 0; alternatingLoadCallback = false;
            Write("alternate-load-requested", new JObject { ["index"] = index, ["path"] = path, ["sha256"] = expected.Sha256 });
            game.SaveManager.AddCallbackAfterLoad(() => alternatingLoadCallback = true);
            game.LoadGame(descriptor);
        }

        private void RequestAlternatingWrite(string name)
        {
            var game = Game.Instance;
            Check(game.SaveManager.IsSaveAllowed(), "actual-native-alternating-save-admission");
            var descriptor = game.SaveManager.CreateNewSave(name);
            Check(descriptor.Name == name && descriptor.Type == SaveInfo.SaveType.Manual && !descriptor.IsActuallySaved,
                "new-exact-native-alternating-slot");
            callback = false;
            Write("alternate-write-requested", new JObject { ["name"] = name, ["nativeType"] = descriptor.Type.ToString() });
            game.SaveGame(descriptor, () => callback = true);
        }

        private bool ObserveAlternatingWrite(string name, string leaf, bool mounted, int count, string label)
        {
            if (!callback || NativePersistenceIsolation.HasPendingWrites) return false;
            var game = Game.Instance;
            var saved = game.SaveManager.SingleOrDefault(s => s.Name == name);
            if (saved == null || saved.OperationState != SaveInfo.StateType.None || !saved.HasFileOnDisk) return false;
            var read = NativeMountedSaveStorage.Read(saved.Saver);
            Check(saved.FileName == leaf && read.Kind == MountedSaveReadKind.Current && read.Data.Mounted == mounted &&
                persistence.SnapshotCount == count, "actual-alternating-native-archive-snapshot");
            Check(relationship.State == (mounted ? RelationshipState.Mounted : RelationshipState.Unmounted) &&
                !persistence.SaveSuspended && !controls.CaptureSnapshot().SerializationSuspended &&
                controls.CaptureSnapshot().DuplicateFactCount == 0 && !game.IsPaused, "alternating-write-retains-live-state");
            if (mounted)
                Check(read.Data.Rider.Id == rider.UniqueId && read.Data.Mount.Id == mount.UniqueId,
                    "alternating-save-native-pair-identities");
            else Check(read.Data.Rider == null && read.Data.Mount == null && read.Data.ProfileId == null,
                "unmounted-save-has-no-pair-record");
            Write("alternate-native-write-complete", new JObject { ["label"] = label, ["ordinal"] = count,
                ["path"] = saved.FolderName, ["sha256"] = Hash(saved.FolderName), ["length"] = new FileInfo(saved.FolderName).Length,
                ["nativeType"] = saved.Type.ToString(), ["nativeCallback"] = callback, ["operation"] = saved.OperationState.ToString(),
                ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
            return true;
        }
    }
}
