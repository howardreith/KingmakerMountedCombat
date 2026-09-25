using System;
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
        private bool FailedLoadCase => ValidationCase && RuntimeRequest.IsFailedLoad(request.PersistenceCase);
        private int failedLoadFrames;
        private int failedLoadSettleFrames;
        private bool failedLoadStopAllRequired;
        private bool failedLoadLoadingCleared;
        private bool failedLoadSawLoading;
        private int failedLoadBeforeDisposals;
        private int failedLoadBeforeRejections;
        private int failedLoadBeforeSemantic;
        private int failedLoadBeforePresentation;
        private int failedLoadFailureSemantic;
        private int failedLoadFailurePresentation;
        private int failedLoadFailureCount;
        private int failedLoadRetryFrames;
        private bool failedLoadRecoveredInSession;
        private string failedLoadBeforeRiderId;
        private SavedNativeActor failedLoadRiderDebt;
        private SavedNativeActor failedLoadMountDebt;

        private void AdvanceFailedLoad()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P06 failed-load timed out at " + validationStage +
                    " frames=" + failedLoadFrames + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (game == null) return;
            stage = 700 + validationStage;
            if (validationStage == 0)
            {
                // Ordinary settled-world preconditions apply only while a world
                // still exists; they must not gate the failure window below.
                if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                    game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "P06-required-policy");
                Check(persistence.LoadedData?.Mounted == true && relationship.State == RelationshipState.Mounted &&
                    persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1,
                    "P06-cold-current-archive-pair");
                Check(persistence.NativeLoadFailureCount == 0, "P06-no-prior-native-load-failure");
                rider = relationship.Rider; mount = relationship.Mount;
                validationRiderId = rider.UniqueId; validationMountId = mount.UniqueId;
                failedLoadBeforeRiderId = validationRiderId;
                failedLoadRiderDebt = MountedPersistenceService.CaptureActor(rider);
                failedLoadMountDebt = MountedPersistenceService.CaptureActor(mount);
                beforeControls = controls.CaptureSnapshot();
                Check(beforeControls.ExactFactCount > 0 && beforeControls.DuplicateFactCount == 0 &&
                    controls.NativeCastRequestCount == 0, "P06-original-native-controls-once");
                failedLoadBeforeDisposals = persistence.NativeWorldDisposalCount;
                failedLoadBeforeRejections = persistence.RejectedLoadCount;
                failedLoadBeforeSemantic = persistence.SemanticRestoreCount;
                failedLoadBeforePresentation = persistence.PresentationRestoreCount;
                Write("initial");
                RequestValidationLoad(request.PersistenceAlternate, "B");
                failedLoadFrames = 0; failedLoadSettleFrames = 0;
                validationStage = 1;
                return;
            }
            if (validationStage == 1)
            {
                // Measured: the corrupt member is unstashed by
                // AreaDataStash.UnstashAreaState inside SceneLoader.LoadAreaCoroutine,
                // a LATER loading process than SaveManager.LoadRoutine. LoadRoutine
                // therefore completes and its own after-load callback legitimately
                // fires; the area load then fails and LoadingProcess.Update rethrows
                // it as LoadGameException. So the callback is not the signal - the
                // absence of a loaded world is. Record every frame before asserting.
                failedLoadFrames++;
                if (LoadingProcess.Instance.IsLoadingInProcess) failedLoadSawLoading = true;
                else failedLoadLoadingCleared = true;
                var settled = failedLoadSawLoading && !LoadingProcess.Instance.IsLoadingInProcess &&
                    (persistence.NativeLoadFailureCount > 0 || game.CurrentlyLoadedArea == null);
                if (!settled)
                {
                    if (failedLoadFrames > 1200)
                        throw new InvalidOperationException("P06 native load neither failed nor completed: loading=" +
                            LoadingProcess.Instance.IsLoadingInProcess + " sawLoading=" + failedLoadSawLoading +
                            " area=" + (game.CurrentlyLoadedArea == null ? "null" : "present") +
                            " failures=" + persistence.NativeLoadFailureCount + " callback=" + validationCallback);
                    return;
                }
                // Let the native pump take its following ticks so the observation
                // covers what the engine does after the failure, not only the failure.
                if (++failedLoadSettleFrames < 12) return;
                // These two references belong to the world the engine destroyed;
                // keeping them would describe actors that no longer exist.
                rider = null; mount = null;
                Write("failed-load-observed", FailedLoadDetail());
                // Multiplicity is not a safety property here and was not measured
                // in advance, so the claim is that a real native failure occurred;
                // the exact count is recorded in the observation instead.
                Check(persistence.NativeLoadFailureCount >= 1 && !string.IsNullOrEmpty(persistence.NativeLoadFailure),
                    "P06-real-native-loading-failure-observed-after-disposal");
                Check(persistence.RejectedLoadCount == failedLoadBeforeRejections,
                    "P06-corrupt-area-member-passed-normal-admission-and-failed-later");
                Check(persistence.NativeWorldDisposalCount == failedLoadBeforeDisposals + 1,
                    "P06-previous-world-was-actually-disposed-before-the-failure");
                // The engine's own after-load callback fires because LoadRoutine
                // itself completed; that is a native signal KMC must not treat as
                // a restored world, so the world state is what is asserted.
                Check(game.CurrentlyLoadedArea == null &&
                    game.CurrentMode == Kingmaker.GameModes.GameModeType.None,
                    "P06-failed-load-completes-no-world-despite-the-native-load-callback");
                // Measured: SaveManager.LoadRoutine completed, so Player.PostLoad
                // ran and early debt restoration legitimately happened before the
                // separate area load failed. Those actors died with the world, so
                // the claim is that the retained selection is INERT, not absent:
                // nothing may be presented into a world that does not exist, and
                // no combat fence may be left behind.
                Check(persistence.PresentationRestoreCount == failedLoadBeforePresentation &&
                    !persistence.CombatRestorationPending,
                    "P06-failed-load-presents-nothing-and-leaves-no-combat-fence");
                Check(persistence.SemanticRestoreCount >= failedLoadBeforeSemantic,
                    "P06-failed-load-never-reduces-restored-actor-accounting");
                failedLoadFailureSemantic = persistence.SemanticRestoreCount;
                failedLoadFailurePresentation = persistence.PresentationRestoreCount;
                failedLoadFailureCount = persistence.NativeLoadFailureCount;
                Check(relationship.State == RelationshipState.Unmounted && relationship.Rider == null &&
                    relationship.Mount == null, "P06-failed-load-leaves-no-actionable-partial-pair");
                Check(NoLiveUnit(failedLoadBeforeRiderId) && NoLiveUnit(validationMountId),
                    "P06-failed-load-leaves-no-stale-live-actor");
                Check(!persistence.SaveSuspended && !NativePersistenceIsolation.HasPendingWrites,
                    "P06-failed-load-releases-owned-save-and-serialization-scopes");
                VerifyValidationArchives();
                // The native pump has no handler around its own MoveNext, so the
                // failed process may still be current. Record whether a reset was
                // genuinely required instead of always issuing one.
                failedLoadStopAllRequired = LoadingProcess.Instance.IsLoadingInProcess;
                if (failedLoadStopAllRequired) LoadingProcess.Instance.StopAll();
                Write("failed-load-recovery-requested", FailedLoadDetail());
                RequestValidationLoad(request.PersistenceLoad, "A");
                validationFrames = 0;
                validationStage = 2;
                return;
            }
            if (validationStage == 2)
            {
                // Measured: once the area load fails after disposal, the Unity
                // scene state is left invalid and SceneLoader.LoadAreaCoroutine
                // throws "Destination scene is not valid" for EVERY later load in
                // this process, including a known-good archive. This window
                // therefore observes which outcome the engine actually produces
                // instead of asserting that a world comes back.
                failedLoadRetryFrames++;
                var recovered = game.CurrentlyLoadedArea != null && !LoadingProcess.Instance.IsLoadingInProcess &&
                    game.CurrentMode == Kingmaker.GameModes.GameModeType.Default && validationCallback;
                var retryFailed = persistence.NativeLoadFailureCount > failedLoadFailureCount &&
                    !LoadingProcess.Instance.IsLoadingInProcess;
                if (!recovered && !retryFailed)
                {
                    if (failedLoadRetryFrames > 1500)
                        throw new InvalidOperationException("P06 in-session retry neither recovered nor failed: loading=" +
                            LoadingProcess.Instance.IsLoadingInProcess + " area=" +
                            (game.CurrentlyLoadedArea == null ? "null" : "present") +
                            " failures=" + persistence.NativeLoadFailureCount);
                    return;
                }
                if (++validationFrames < 12) return;
                failedLoadRecoveredInSession = recovered;
                VerifyValidationArchives();
                Check(!recovered || persistence.NativeLoadFailureCount == failedLoadFailureCount,
                    "P06-an-actually-recovered-world-adds-no-further-native-failure");
                // Whatever the engine did, nothing may be fabricated: no pair may
                // appear without a world, and the archives must be untouched.
                Check(recovered || (game.CurrentlyLoadedArea == null &&
                        relationship.State == RelationshipState.Unmounted &&
                        relationship.Rider == null && relationship.Mount == null &&
                        persistence.PresentationRestoreCount == failedLoadFailurePresentation),
                    "P06-failed-in-session-retry-presents-no-pair-and-invents-no-world");
                Check(!persistence.SaveSuspended && !NativePersistenceIsolation.HasPendingWrites,
                    "P06-retry-leaves-no-owned-save-or-serialization-scope-held");
                Write("failed-load-retry-observed", FailedLoadDetail());
                Dispose();
                Result = new RuntimeSubscenarioResult { Name = request.Scenario, Status = "PASS",
                    AssertionPassCount = passed, AssertionFailCount = 0, Errors = new string[0] };
                Completed = true;
            }
        }

        // After disposal the world may be gone entirely, so every lookup here has
        // to tolerate a null game state rather than assume one survived.
        private static bool NoLiveUnit(string id)
        {
            var units = Game.Instance?.State?.Units;
            if (units == null || string.IsNullOrEmpty(id)) return true;
            return !units.Any(u => u != null && u.UniqueId == id);
        }

        private JObject FailedLoadDetail()
        {
            var game = Game.Instance;
            int? facts = null, duplicates = null, slots = null;
            try
            {
                var snapshot = controls.CaptureSnapshot();
                facts = snapshot.ExactFactCount; duplicates = snapshot.DuplicateFactCount;
                slots = snapshot.ManagedHotbarSlotCount;
            }
            catch (Exception exception) { logger.Exception("Failed-load control observation unavailable", exception); }
            return new JObject {
                ["case"] = request.PersistenceCase,
                ["corruptedMember"] = request.PersistenceAlternate?.Area + ".json",
                ["frames"] = failedLoadFrames, ["settleFrames"] = failedLoadSettleFrames,
                ["nativeLoadFailures"] = persistence.NativeLoadFailureCount,
                ["nativeLoadFailure"] = persistence.NativeLoadFailure,
                ["afterLoadCallback"] = validationCallback,
                ["loadingInProcess"] = LoadingProcess.Instance.IsLoadingInProcess,
                ["loadingClearedItself"] = failedLoadLoadingCleared,
                ["sawLoading"] = failedLoadSawLoading,
                ["retryFrames"] = failedLoadRetryFrames,
                ["recoveredInSession"] = failedLoadRecoveredInSession,
                ["stopAllRequired"] = failedLoadStopAllRequired,
                ["gameMode"] = game?.CurrentMode.ToString(),
                ["currentAreaNull"] = game?.CurrentlyLoadedArea == null,
                ["playerNull"] = game?.Player == null,
                ["unitCount"] = game?.State?.Units == null ? -1 : game.State.Units.Count(),
                ["rejections"] = persistence.RejectedLoadCount,
                ["nativeWorldDisposals"] = persistence.NativeWorldDisposalCount,
                ["semantic"] = persistence.SemanticRestoreCount,
                ["presentation"] = persistence.PresentationRestoreCount,
                ["loadedDataNull"] = persistence.LoadedData == null,
                ["combatRestorationPending"] = persistence.CombatRestorationPending,
                ["saveSuspended"] = persistence.SaveSuspended,
                ["pendingWrites"] = NativePersistenceIsolation.HasPendingWrites,
                ["relationship"] = relationship.State.ToString(),
                ["exactFactCount"] = facts, ["duplicateFactCount"] = duplicates, ["managedSlotCount"] = slots,
                ["feedback"] = persistence.Feedback,
                ["sourceHash"] = request.PersistenceLoad.Sha256, ["variantHash"] = request.PersistenceAlternate.Sha256
            };
        }
    }
}
