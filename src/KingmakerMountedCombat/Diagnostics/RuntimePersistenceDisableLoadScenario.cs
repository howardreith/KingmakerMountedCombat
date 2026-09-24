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
        // The registered disable during a REAL native load of a mounted save,
        // probed through the exact UMM toggle at every frame the load owns:
        // before early restoration, and at the semantic-restored /
        // presentation-pending boundary. Then the equivalent-state cycle at
        // rest, and a second load whose disable is requested in the same frame
        // as the load, before the routine has started -- recorded as what the
        // production path actually does, and checked for a safe outcome either
        // way.
        private bool DisableLoadCase => request.Scenario == "persistence-p07-save" && request.PersistenceCase == "disable-during-load";
        private int disableLoadStage;
        private int disableLoadFrames;
        private string disableLoadRiderId;
        private string disableLoadMountId;
        private int disableLoadSemanticsBefore;
        private int disableLoadPresentationBefore;
        private int disableLoadFactsMounted;
        private SaveInfo disableLoadArchive;
        private string disableLoadArchiveHash;
        private readonly Dictionary<string, int> disableLoadProbes = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> disableLoadRefusals = new Dictionary<string, int>(StringComparer.Ordinal);
        private int disableLoadOutsideFrames;
        private bool disableLoadEnabledThroughout = true;
        private bool disableLoadRestDisabled;
        private bool disableLoadRestReEnabled;
        private bool disableLoadRestRemounted;
        private int disableLoadFactsDisabled;
        private int disableLoadFactsReEnabled;
        private bool disableLoadPreRoutineAccepted;
        private bool disableLoadPreRoutineInFlight;
        private bool disableLoadPreRoutineLoading;
        private int disableLoadSemanticsAtSecond;
        private int disableLoadPresentationAtSecond;
        private string disableLoadStateAfterSecond;
        private bool disableLoadSecondReEnabled;
        private bool disableLoadSecondRemounted;

        private const string PhasePending = "semantic-restored-presentation-pending";
        private const string PhaseBefore = "loading-before-semantic-restore";

        private void AdvanceDisableLoad()
        {
            if (clock.Elapsed.TotalSeconds > 300)
                throw new InvalidOperationException("P07 disable-during-load timed out at " + disableLoadStage +
                    " frames=" + disableLoadFrames + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (game == null) return;
            stage = 1100 + disableLoadStage;
            if (disableLoadStage == 1 || disableLoadStage == 3)
            {
                disableLoadFrames++;
                var loadDone = callback && !LoadingProcess.Instance.IsLoadingInProcess && game.CurrentlyLoadedArea != null &&
                    game.CurrentMode == Kingmaker.GameModes.GameModeType.Default && !NativePersistenceIsolation.HasPendingWrites;
                if (!loadDone)
                {
                    if (disableLoadFrames > 7200) throw new InvalidOperationException("P07 disable-during-load: the native load never completed.");
                    if (disableLoadStage == 1 && disableLoadFrames % 3 == 0) ProbeDisableDuringLoad();
                    return;
                }
                if (disableLoadFrames < 10) return;
                if (disableLoadStage == 1) { CompleteFirstDisableLoad(); return; }
                CompleteSecondDisableLoad();
                return;
            }
            if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            if (disableLoadStage == 0)
            {
                if (!callback || NativePersistenceIsolation.HasPendingWrites) return;
                Check(relationship.State == RelationshipState.Mounted && persistence.SnapshotCount == 1,
                    "P07-disable-load-opens-with-one-real-mounted-save");
                rider = relationship.Rider; mount = relationship.Mount;
                disableLoadRiderId = rider.UniqueId; disableLoadMountId = mount.UniqueId;
                beforeControls = controls.CaptureSnapshot();
                disableLoadFactsMounted = beforeControls.ExactFactCount;
                disableLoadArchive = RecoveryArchive();
                disableLoadArchiveHash = Hash(disableLoadArchive.FolderName);
                var read = NativeMountedSaveStorage.Read(disableLoadArchive.Saver);
                Check(read.Kind == MountedSaveReadKind.Current && read.Data.Mounted &&
                    read.Data.Rider.Id == disableLoadRiderId && read.Data.Mount.Id == disableLoadMountId,
                    "P07-disable-load-archive-carries-the-pair");
                Write("native-write-complete", new JObject {
                    ["ordinal"] = 1, ["path"] = disableLoadArchive.FolderName, ["sha256"] = disableLoadArchiveHash,
                    ["length"] = new FileInfo(disableLoadArchive.FolderName).Length, ["nativeType"] = disableLoadArchive.Type.ToString(),
                    ["nativeCallback"] = callback, ["operation"] = disableLoadArchive.OperationState.ToString(),
                    ["snapshot"] = JObject.FromObject(read.Data, MountedSaveCodec.CreateSerializer()) });
                disableLoadSemanticsBefore = persistence.SemanticRestoreCount;
                disableLoadPresentationBefore = persistence.PresentationRestoreCount;
                Write("disable-load-requested", DisableLoadDetail(null));
                RequestRecoveryLoad(disableLoadArchive);
                disableLoadFrames = 0; disableLoadStage = 1;
                return;
            }
            if (disableLoadStage == 2)
            {
                // The equivalent-state cycle at rest: disable, re-enable, remount.
                disableLoadRestDisabled = Main.InvokeRegisteredToggleForAutomation(false);
                disableLoadFactsDisabled = controls.CaptureSnapshot().ExactFactCount;
                var stateDisabled = relationship.State;
                disableLoadRestReEnabled = Main.InvokeRegisteredToggleForAutomation(true);
                var stateReEnabled = relationship.State;
                disableLoadRestRemounted = relationship.MountAutomationPair().Succeeded && relationship.State == RelationshipState.Mounted;
                rider = relationship.Rider; mount = relationship.Mount;
                var after = controls.CaptureSnapshot();
                disableLoadFactsReEnabled = after.ExactFactCount;
                Write("disable-load-rest-cycle", DisableLoadDetail(new JObject {
                    ["stateDisabled"] = stateDisabled.ToString(), ["stateReEnabled"] = stateReEnabled.ToString() }));
                Check(disableLoadRestDisabled && stateDisabled == RelationshipState.Unmounted && disableLoadFactsDisabled < disableLoadFactsMounted,
                    "P07-registered-disable-succeeds-at-rest-after-the-load");
                Check(disableLoadRestReEnabled && stateReEnabled == RelationshipState.Unmounted && disableLoadRestRemounted &&
                    rider.UniqueId == disableLoadRiderId && mount.UniqueId == disableLoadMountId &&
                    after.DuplicateFactCount == 0 && disableLoadFactsReEnabled <= disableLoadFactsMounted &&
                    disableLoadFactsReEnabled > disableLoadFactsDisabled && controls.NativeCastRequestCount == 0,
                    "P07-re-enable-returns-to-an-equivalent-mounted-state-with-the-same-actors");
                // A second real load with the disable requested in the SAME frame,
                // before the routine has started. Recorded, then judged by outcome.
                disableLoadSemanticsAtSecond = persistence.SemanticRestoreCount;
                disableLoadPresentationAtSecond = persistence.PresentationRestoreCount;
                var archive = game.SaveManager.Single(s => s.FolderName == disableLoadArchive.FolderName);
                RequestRecoveryLoad(archive);
                disableLoadPreRoutineLoading = LoadingProcess.Instance.IsLoadingInProcess;
                disableLoadPreRoutineInFlight = persistence.LoadInFlight;
                disableLoadPreRoutineAccepted = Main.InvokeRegisteredToggleForAutomation(false);
                Write("disable-load-preroutine-probe", DisableLoadDetail(null));
                Check(!disableLoadPreRoutineAccepted || !disableLoadPreRoutineInFlight,
                    "P07-a-disable-is-never-accepted-while-the-load-owns-the-world");
                disableLoadFrames = 0; disableLoadStage = 3;
            }
        }

        private void ProbeDisableDuringLoad()
        {
            if (!persistence.LoadInFlight) { disableLoadOutsideFrames++; return; }
            var phase = persistence.SemanticRestoreCount > disableLoadSemanticsBefore ? PhasePending : PhaseBefore;
            int count;
            disableLoadProbes.TryGetValue(phase, out count); disableLoadProbes[phase] = count + 1;
            // The exact registered UMM toggle, while the load owns the world.
            var refused = !Main.InvokeRegisteredToggleForAutomation(false);
            if (refused) { disableLoadRefusals.TryGetValue(phase, out count); disableLoadRefusals[phase] = count + 1; }
            if (!persistence.Enabled) disableLoadEnabledThroughout = false;
            if (!refused)
                throw new InvalidOperationException("P07 registered disable was accepted during a live mounted load at " + phase + ".");
        }

        private void CompleteFirstDisableLoad()
        {
            var game = Game.Instance;
            int pending; disableLoadProbes.TryGetValue(PhasePending, out pending);
            int pendingRefused; disableLoadRefusals.TryGetValue(PhasePending, out pendingRefused);
            int before; disableLoadProbes.TryGetValue(PhaseBefore, out before);
            int beforeRefused; disableLoadRefusals.TryGetValue(PhaseBefore, out beforeRefused);
            Write("disable-load-probed", DisableLoadDetail(null));
            Check(pending >= 1 && pendingRefused == pending && beforeRefused == before && disableLoadEnabledThroughout &&
                persistence.Enabled, "P07-registered-disable-is-refused-at-every-live-load-frame-including-presentation-pending");
            Check(relationship.State == RelationshipState.Mounted && relationship.Rider.UniqueId == disableLoadRiderId &&
                relationship.Mount.UniqueId == disableLoadMountId &&
                persistence.SemanticRestoreCount == disableLoadSemanticsBefore + 2 &&
                persistence.PresentationRestoreCount == disableLoadPresentationBefore + 1 &&
                controls.NativeCastRequestCount == 0, "P07-refused-disables-left-the-load-to-restore-the-pair-exactly-once");
            rider = relationship.Rider; mount = relationship.Mount;
            var after = controls.CaptureSnapshot();
            Check(after.ExactFactCount == disableLoadFactsMounted && after.DuplicateFactCount == 0 && !after.SerializationSuspended,
                "P07-controls-are-present-exactly-once-after-the-probed-load");
            Check(Hash(disableLoadArchive.FolderName) == disableLoadArchiveHash, "P07-probed-load-left-the-archive-byte-identical");
            disableLoadFrames = 0; disableLoadStage = 2;
        }

        private void CompleteSecondDisableLoad()
        {
            var game = Game.Instance;
            disableLoadStateAfterSecond = relationship.State.ToString();
            var semanticsDelta = persistence.SemanticRestoreCount - disableLoadSemanticsAtSecond;
            var presentationDelta = persistence.PresentationRestoreCount - disableLoadPresentationAtSecond;
            if (disableLoadPreRoutineAccepted)
            {
                // Disabled before the routine started: the engine opened the save
                // with no KMC restoration at all, and nothing was cast or invented.
                Check(relationship.State == RelationshipState.Unmounted && !persistence.Enabled && semanticsDelta == 0 &&
                    presentationDelta == 0 && controls.NativeCastRequestCount == 0,
                    "P07-a-load-under-a-disabled-KMC-restores-nothing-and-opens-cleanly");
                disableLoadSecondReEnabled = Main.InvokeRegisteredToggleForAutomation(true);
                Check(disableLoadSecondReEnabled && relationship.State == RelationshipState.Unmounted,
                    "P07-re-enabling-after-that-load-invents-no-pair-retroactively");
            }
            else
            {
                Check(relationship.State == RelationshipState.Mounted && persistence.Enabled && semanticsDelta == 2 &&
                    presentationDelta == 1 && controls.NativeCastRequestCount == 0,
                    "P07-a-refused-pre-routine-disable-left-the-load-to-restore-the-pair-once");
                disableLoadSecondReEnabled = true;
                disableLoadSecondReEnabled = Main.InvokeRegisteredToggleForAutomation(false) && Main.InvokeRegisteredToggleForAutomation(true);
                Check(disableLoadSecondReEnabled && relationship.State == RelationshipState.Unmounted,
                    "P07-the-rest-cycle-repeats-after-the-second-load");
            }
            disableLoadSecondRemounted = relationship.MountAutomationPair().Succeeded && relationship.State == RelationshipState.Mounted;
            rider = relationship.Rider; mount = relationship.Mount;
            Check(disableLoadSecondRemounted && rider.UniqueId == disableLoadRiderId && mount.UniqueId == disableLoadMountId &&
                controls.CaptureSnapshot().DuplicateFactCount == 0 && controls.NativeCastRequestCount == 0,
                "P07-remount-after-the-second-load-uses-the-same-actors-once");
            Check(Hash(disableLoadArchive.FolderName) == disableLoadArchiveHash, "P07-second-load-left-the-archive-byte-identical");
            beforeControls = controls.CaptureSnapshot();
            Write("disable-load-second-load", DisableLoadDetail(new JObject { ["semanticsDelta"] = semanticsDelta, ["presentationDelta"] = presentationDelta }));
            recoveryContinuation = true; stage = 2;
        }

        private JObject DisableLoadDetail(JObject extra)
        {
            var probes = new JObject(); foreach (var pair in disableLoadProbes) probes[pair.Key] = pair.Value;
            var refusals = new JObject(); foreach (var pair in disableLoadRefusals) refusals[pair.Key] = pair.Value;
            var detail = new JObject {
                ["case"] = request.PersistenceCase, ["stage"] = disableLoadStage, ["frames"] = disableLoadFrames,
                ["riderId"] = disableLoadRiderId, ["mountId"] = disableLoadMountId,
                ["archivePath"] = disableLoadArchive?.FolderName, ["archiveSha256"] = disableLoadArchiveHash,
                ["probes"] = probes, ["refusals"] = refusals, ["outsideFrames"] = disableLoadOutsideFrames,
                ["enabledThroughout"] = disableLoadEnabledThroughout, ["enabled"] = persistence.Enabled,
                ["loadInFlight"] = persistence.LoadInFlight, ["loading"] = LoadingProcess.Instance.IsLoadingInProcess,
                ["semanticsBefore"] = disableLoadSemanticsBefore, ["presentationBefore"] = disableLoadPresentationBefore,
                ["semantics"] = persistence.SemanticRestoreCount, ["presentation"] = persistence.PresentationRestoreCount,
                ["factsMounted"] = disableLoadFactsMounted, ["factsDisabled"] = disableLoadFactsDisabled,
                ["factsReEnabled"] = disableLoadFactsReEnabled,
                ["restDisabled"] = disableLoadRestDisabled, ["restReEnabled"] = disableLoadRestReEnabled,
                ["restRemounted"] = disableLoadRestRemounted,
                ["preRoutineAccepted"] = disableLoadPreRoutineAccepted, ["preRoutineInFlight"] = disableLoadPreRoutineInFlight,
                ["preRoutineLoading"] = disableLoadPreRoutineLoading,
                ["semanticsAtSecond"] = disableLoadSemanticsAtSecond, ["presentationAtSecond"] = disableLoadPresentationAtSecond,
                ["stateAfterSecond"] = disableLoadStateAfterSecond, ["secondReEnabled"] = disableLoadSecondReEnabled,
                ["secondRemounted"] = disableLoadSecondRemounted,
                ["nativeCastRequests"] = controls.NativeCastRequestCount, ["snapshots"] = persistence.SnapshotCount,
                ["failedSaves"] = persistence.FailedSaveCount, ["rejections"] = persistence.RejectedLoadCount,
                ["disposals"] = persistence.NativeWorldDisposalCount, ["feedback"] = persistence.Feedback };
            if (extra != null) foreach (var property in extra.Properties()) detail[property.Name] = property.Value;
            return detail;
        }
    }
}
