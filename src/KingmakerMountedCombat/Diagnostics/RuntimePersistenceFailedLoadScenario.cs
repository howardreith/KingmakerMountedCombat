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
        private int failedLoadBeforeDisposals;
        private int failedLoadBeforeRejections;
        private int failedLoadBeforeSemantic;
        private int failedLoadBeforePresentation;
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
                // The corrupt area member fails inside ThreadedGameLoader, after
                // Game.DisposeState already destroyed the previous world, so there
                // is no loaded area and the native pump has no handler of its own.
                // Record every frame before asserting anything.
                failedLoadFrames++;
                if (!LoadingProcess.Instance.IsLoadingInProcess) failedLoadLoadingCleared = true;
                if (persistence.NativeLoadFailureCount == 0)
                {
                    if (failedLoadFrames > 1200)
                        throw new InvalidOperationException("P06 native load neither failed nor completed: loading=" +
                            LoadingProcess.Instance.IsLoadingInProcess + " area=" +
                            (game.CurrentlyLoadedArea == null ? "null" : "present") + " callback=" + validationCallback);
                    return;
                }
                // Let the native pump take its following ticks so the observation
                // covers what the engine does after the throw, not only the throw.
                if (++failedLoadSettleFrames < 12) return;
                // These two references belong to the world the engine destroyed;
                // keeping them would describe actors that no longer exist.
                rider = null; mount = null;
                Write("failed-load-observed", FailedLoadDetail());
                Check(persistence.NativeLoadFailureCount == 1 && !string.IsNullOrEmpty(persistence.NativeLoadFailure),
                    "P06-exactly-one-real-native-load-failure-after-disposal");
                Check(persistence.RejectedLoadCount == failedLoadBeforeRejections,
                    "P06-corrupt-area-member-passed-normal-admission-and-failed-later");
                Check(persistence.NativeWorldDisposalCount == failedLoadBeforeDisposals + 1,
                    "P06-previous-world-was-actually-disposed-before-the-failure");
                Check(!validationCallback, "P06-failed-load-reports-no-successful-load-callback");
                Check(game.CurrentlyLoadedArea == null, "P06-failed-load-leaves-no-completed-world");
                Check(persistence.LoadedData == null && !persistence.CombatRestorationPending,
                    "P06-failed-load-retains-no-selected-metadata-or-combat-fence");
                Check(persistence.SemanticRestoreCount == failedLoadBeforeSemantic &&
                    persistence.PresentationRestoreCount == failedLoadBeforePresentation,
                    "P06-failed-load-restores-no-actor-or-presentation");
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
                if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                    game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
                if (!validationCallback || ++validationFrames < 10) return;
                Check(persistence.NativeLoadFailureCount == 1,
                    "P06-valid-retry-adds-no-further-native-load-failure");
                Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Rider.UniqueId == validationRiderId && relationship.Mount.UniqueId == validationMountId,
                    "P06-valid-retry-after-failed-load-restores-the-actual-pair");
                rider = relationship.Rider; mount = relationship.Mount;
                Check(persistence.SemanticRestoreCount == failedLoadBeforeSemantic + 2 &&
                    persistence.PresentationRestoreCount == failedLoadBeforePresentation + 1,
                    "P06-valid-retry-restores-only-A-once");
                Check(controls.CaptureSnapshot().ExactFactCount == beforeControls.ExactFactCount &&
                    controls.CaptureSnapshot().DuplicateFactCount == 0 && controls.NativeCastRequestCount == 0 &&
                    !controls.CaptureSnapshot().SerializationSuspended,
                    "P06-valid-retry-controls-without-acquisition-or-duplication");
                Check(LegitimateContinuation(failedLoadRiderDebt, MountedPersistenceService.CaptureActor(rider), 0) &&
                    LegitimateContinuation(failedLoadMountDebt, MountedPersistenceService.CaptureActor(mount), 0),
                    "P06-valid-retry-restores-actual-saved-expenditure");
                Check(!game.Player.IsInCombat && !game.IsPaused, "P06-recovered-native-world-usable");
                VerifyValidationArchives();
                Write("validation-valid-retry", FailedLoadDetail());
                validationContinuation = true; stage = 2;
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
                ["stopAllRequired"] = failedLoadStopAllRequired,
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
