using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        private bool ValidationCase => request.Scenario == "persistence-p06-load";
        private bool validationContinuation;
        private int validationStage;
        private int validationFrames;
        private bool validationCallback;
        private string validationRiderId;
        private string validationMountId;
        private int validationSemantic;
        private int validationPresentation;
        private Player validationPreviousPlayer;

        private bool ValidationRefused => request.PersistenceCase == "future" || request.PersistenceCase == "malformed" ||
            request.PersistenceCase == "profile" || request.PersistenceCase == "campaign" || request.PersistenceCase == "policy";
        private bool ValidationPair => request.PersistenceCase == "schema1";

        private void AdvanceValidation()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P06 native boundary timed out: " + validationStage);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null ||
                game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            stage = 300 + validationStage;
            if (validationStage == 0)
            {
                Check(settings.EnablePairedActivation && !settings.EnableUnifiedMountedTurn &&
                    !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay, "P06-required-policy");
                Check(persistence.LoadedData?.Mounted == true && relationship.State == RelationshipState.Mounted &&
                    persistence.SemanticRestoreCount == 2 && persistence.PresentationRestoreCount == 1,
                    "P06-cold-current-archive-pair");
                rider = relationship.Rider; mount = relationship.Mount;
                validationRiderId = rider.UniqueId; validationMountId = mount.UniqueId;
                beforeControls = controls.CaptureSnapshot();
                Check(beforeControls.ExactFactCount > 0 && beforeControls.DuplicateFactCount == 0 &&
                    controls.NativeCastRequestCount == 0, "P06-original-native-controls-once");
                Write("initial");
                if (ValidationRefused)
                {
                    VerifyRejectedNativeEntries();
                    Write("validation-original-world-retained", ValidationDetail());
                    validationContinuation = true; stage = 2;
                    return;
                }
                RequestValidationLoad(request.PersistenceAlternate, "B");
                validationStage = 1;
                return;
            }
            if (validationStage == 1)
            {
                if (!validationCallback || ++validationFrames < 10) return;
                Check(!ReferenceEquals(validationPreviousPlayer, game.Player), "P06-native-B-world-replaced");
                rider = game.State.Units.Single(u => u.UniqueId == validationRiderId);
                mount = game.State.Units.Single(u => u.UniqueId == validationMountId);
                var expectedSemantics = request.PersistenceCase == "legacy" ? 2 :
                    request.PersistenceCase == "missing-rider" || request.PersistenceCase == "missing-mount" ? 3 : 4;
                Check(persistence.SemanticRestoreCount == expectedSemantics &&
                    persistence.PresentationRestoreCount == (ValidationPair ? 2 : 1), "P06-no-invented-grant-or-presentation");
                Check(relationship.State == (ValidationPair ? RelationshipState.Mounted : RelationshipState.Unmounted),
                    "P06-variant-native-relationship: " + persistence.Feedback);
                Check(controls.NativeCastRequestCount == 0 && controls.CaptureSnapshot().DuplicateFactCount == 0 &&
                    !persistence.SaveSuspended && !controls.CaptureSnapshot().SerializationSuspended,
                    "P06-no-Mount-replay-or-stuck-controls");
                if (request.PersistenceCase == "legacy")
                    Check(persistence.LoadedData == null && controls.CapturePersistentSlots().Length == 0,
                        "P06-legacy-load-drops-A-metadata-and-owned-slots");
                else
                    Check(persistence.LoadedData?.SchemaVersion == 2 && persistence.LoadedData.Combat == null,
                        "P06-bounded-current-or-in-memory-migration-without-combat");
                Check(!game.Player.IsInCombat && !game.IsPaused, "P06-native-world-usable-after-variant");
                VerifyValidationArchives();
                Write("validation-variant-loaded", ValidationDetail());
                validationSemantic = persistence.SemanticRestoreCount;
                validationPresentation = persistence.PresentationRestoreCount;
                RequestValidationLoad(request.PersistenceLoad, "A");
                validationStage = 2;
                return;
            }
            if (validationStage == 2)
            {
                if (!validationCallback || ++validationFrames < 10) return;
                Check(!ReferenceEquals(validationPreviousPlayer, game.Player), "P06-native-A-world-replaced-again");
                Check(relationship.State == RelationshipState.Mounted &&
                    relationship.Rider.UniqueId == validationRiderId && relationship.Mount.UniqueId == validationMountId &&
                    persistence.SemanticRestoreCount == validationSemantic + 2 &&
                    persistence.PresentationRestoreCount == validationPresentation + 1, "P06-valid-retry-restores-only-A-once");
                rider = relationship.Rider; mount = relationship.Mount;
                Check(controls.CaptureSnapshot().ExactFactCount == beforeControls.ExactFactCount &&
                    controls.CaptureSnapshot().DuplicateFactCount == 0 && controls.NativeCastRequestCount == 0,
                    "P06-valid-retry-controls-without-acquisition");
                VerifyValidationArchives();
                Write("validation-valid-retry", ValidationDetail());
                validationContinuation = true; stage = 2;
            }
        }

        private void VerifyRejectedNativeEntries()
        {
            var game = Game.Instance;
            var selected = game.SaveManager.Single(s => s.FileName == request.PersistenceAlternate.FileName);
            var player = game.Player; var world = game.State; var data = persistence.LoadedData;
            var priorRider = rider; var priorMount = mount;
            var nativeRider = MountedPersistenceService.CaptureActor(rider);
            var nativeMount = MountedPersistenceService.CaptureActor(mount);
            var rejects = persistence.RejectedLoadCount;
            var semantic = persistence.SemanticRestoreCount;
            var presentation = persistence.PresentationRestoreCount;
            var paused = game.IsPaused;
            var selectedUnits = SelectionManager.Instance.SelectedUnits.ToArray();
            var originalPolicy = settings.EnablePairedActivation;
            try
            {
                if (request.PersistenceCase == "policy") settings.EnablePairedActivation = false;
                for (var index = 0; index < 3; index++)
                {
                    if (index == 0) game.LoadGameFromMainMenu(selected);
                    else if (index == 1) game.LoadGame(selected);
                    else game.LoadGameForSmokeTest(selected);
                    Check(persistence.RejectedLoadCount == rejects + index + 1 &&
                        !LoadingProcess.Instance.IsLoadingInProcess, "P06-native-entry-refused-before-enumeration");
                    Check(ReferenceEquals(player, game.Player) && ReferenceEquals(world, game.State) &&
                        ReferenceEquals(data, persistence.LoadedData) && relationship.Rider == priorRider &&
                        relationship.Mount == priorMount && relationship.State == RelationshipState.Mounted,
                        "P06-refusal-preserves-existing-native-world");
                    Check(persistence.SemanticRestoreCount == semantic && persistence.PresentationRestoreCount == presentation &&
                        controls.CaptureSnapshot().ExactFactCount == beforeControls.ExactFactCount &&
                        controls.CaptureSnapshot().DuplicateFactCount == 0 &&
                        SelectionManager.Instance.SelectedUnits.SequenceEqual(selectedUnits) && game.IsPaused == paused,
                        "P06-refusal-preserves-controls-selection-and-restoration-history");
                    Check(LegitimateContinuation(nativeRider, MountedPersistenceService.CaptureActor(rider), 0) &&
                        LegitimateContinuation(nativeMount, MountedPersistenceService.CaptureActor(mount), 0),
                        "P06-refusal-changes-no-native-action-debt");
                    VerifyValidationArchives();
                    Write("validation-native-load-refused", new JObject { ["entry"] = index,
                        ["rejections"] = persistence.RejectedLoadCount, ["feedback"] = persistence.Feedback,
                        ["path"] = selected.FolderName, ["sha256"] = Hash(selected.FolderName) });
                }
            }
            finally { settings.EnablePairedActivation = originalPolicy; }
        }

        private void RequestValidationLoad(RuntimeSaveDescriptor expected, string label)
        {
            var game = Game.Instance;
            VerifyValidationArchives();
            var descriptor = game.SaveManager.Single(s => s.FileName == expected.FileName);
            Check(descriptor.GameId == expected.GameId && descriptor.Name == expected.InternalName &&
                descriptor.Type == SaveInfo.SaveType.Manual, "P06-exact-native-variant-descriptor");
            validationPreviousPlayer = game.Player;
            validationCallback = false; validationFrames = 0;
            Write("validation-native-load-requested", new JObject { ["label"] = label,
                ["path"] = descriptor.FolderName, ["sha256"] = Hash(descriptor.FolderName) });
            game.SaveManager.AddCallbackAfterLoad(() => validationCallback = true);
            game.LoadGameFromMainMenu(descriptor);
        }

        private void VerifyValidationArchives()
        {
            foreach (var descriptor in new[] { request.PersistenceLoad, request.PersistenceAlternate })
                Check(Hash(Path.Combine(Game.Instance.SaveManager.SavePath, descriptor.FileName)) == descriptor.Sha256,
                    "P06-original-and-derived-archives-unchanged");
        }

        private JObject ValidationDetail() => new JObject {
            ["case"] = request.PersistenceCase, ["rejections"] = persistence.RejectedLoadCount,
            ["nativeCallback"] = validationCallback, ["semantic"] = persistence.SemanticRestoreCount,
            ["presentation"] = persistence.PresentationRestoreCount, ["feedback"] = persistence.Feedback,
            ["mounted"] = relationship.State == RelationshipState.Mounted,
            ["sourceHash"] = request.PersistenceLoad.Sha256, ["variantHash"] = request.PersistenceAlternate.Sha256
        };
    }
}
