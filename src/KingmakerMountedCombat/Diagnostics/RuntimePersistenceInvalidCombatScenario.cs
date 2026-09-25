using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class RuntimePersistenceScenario
    {
        internal static bool IsCombatValidation(string value) => value == "combat-missing" || value == "combat-ai";
        private bool ValidationCombatCase => ValidationCase && IsCombatValidation(request.PersistenceCase);
        private int validationInitialActors;
        private bool validationCanceledCallback;
        private int invalidCombatNativeProbes;
        private int invalidCombatDuplicateNotifications;
        private SavedNativeActor validationFailedRider;
        private SavedNativeActor validationFailedMount;

        private void AdvanceInvalidCombat()
        {
            if (clock.Elapsed.TotalSeconds > 150)
                throw new InvalidOperationException("P06 invalid combat timed out at " + validationStage + ": " + persistence.Feedback);
            var game = Game.Instance;
            if (LoadingProcess.Instance.IsLoadingInProcess || game.CurrentlyLoadedArea == null) return;
            if (game.IsPaused) { game.IsPaused = false; return; }
            if (game.CurrentMode != Kingmaker.GameModes.GameModeType.Default) return;
            stage = 600 + validationStage;
            if (validationStage == 0)
            {
                var data = persistence.LoadedData;
                Check(data?.Combat?.TurnBased == true && relationship.State == RelationshipState.Mounted &&
                    !persistence.CombatRestorationPending && settings.EnablePairedActivation &&
                    !settings.EnableUnifiedMountedTurn && !settings.EnablePairedCommandScheduler &&
                    !settings.EnableDiagnosticOverlay, "P06-cold-valid-combat-from-actual-native-archive");
                rider = relationship.Rider; mount = relationship.Mount;
                validationRiderId = rider.UniqueId; validationMountId = mount.UniqueId;
                validationInitialActors = data.Combat.Actors.Length;
                Check(persistence.SemanticRestoreCount == validationInitialActors &&
                    persistence.PresentationRestoreCount == 1, "P06-cold-valid-actors-and-pair-once");
                beforeControls = controls.CaptureSnapshot();
                Write("initial", CombatObservation());
                RequestValidationLoad(request.PersistenceAlternate, "B");
                validationStage = 1;
                return;
            }
            if (validationStage == 1)
            {
                if (!validationCallback || ++validationFrames < 10) return;
                Check(!ReferenceEquals(validationPreviousPlayer, game.Player) &&
                    persistence.NativeWorldDisposalCount == 1, "P06-damaged-combat-loaded-through-native-world-replacement");
                rider = game.State.Units.Single(u => u.UniqueId == validationRiderId);
                mount = game.State.Units.Single(u => u.UniqueId == validationMountId);
                var data = persistence.LoadedData;
                Check(data?.Combat != null && persistence.CombatRestorationPending &&
                    persistence.Feedback.Contains("restoration is blocked") &&
                    relationship.State == RelationshipState.Unmounted, "P06-invalid-participation-fails-clearly-without-remount");
                var expected = validationInitialActors * 2 - (request.PersistenceCase == "combat-missing" ? 1 : 0);
                Check(persistence.SemanticRestoreCount == expected && persistence.PresentationRestoreCount == 1 &&
                    controls.NativeCastRequestCount == 0, "P06-invalid-combat-does-not-invent-actor-or-preparation");
                Check(data.Combat.Actors.All(a => {
                    var units = game.State.Units.Where(u => u.UniqueId == a.Native.Id).ToArray();
                    return units.Length == 0 || units.Length == 1 &&
                        LegitimateContinuation(a.Native, MountedPersistenceService.CaptureActor(units[0]), 0);
                }), "P06-resolvable-current-native-expenditure-survives-invalid-semantics");
                validationFailedRider = MountedPersistenceService.CaptureActor(rider);
                validationFailedMount = MountedPersistenceService.CaptureActor(mount);
                VerifyBlockedNativeCombat();
                VerifyValidationArchives();
                Write("validation-combat-blocked", InvalidCombatDetail());
                validationPreviousPlayer = game.Player;
                var selected = game.SaveManager.Single(s => s.FileName == request.PersistenceLoad.FileName);
                var queued = game.SaveManager.LoadRoutine(selected, false);
                LoadingProcess.Instance.StartLoadingProcess(queued, () => validationCanceledCallback = true);
                LoadingProcess.Instance.StopAll();
                validationFrames = 0; validationStage = 2;
                return;
            }
            if (validationStage == 2)
            {
                if (++validationFrames < 4) return;
                Check(ReferenceEquals(validationPreviousPlayer, game.Player) && !validationCanceledCallback &&
                    persistence.NativeWorldDisposalCount == 1 && persistence.LoadedData == null &&
                    persistence.CombatRestorationPending, "P07-canceled-unstarted-replacement-retains-failed-world-fence");
                VerifyBlockedNativeCombat();
                VerifyValidationArchives();
                Write("validation-canceled-replacement", InvalidCombatDetail());
                validationSemantic = persistence.SemanticRestoreCount;
                validationPresentation = persistence.PresentationRestoreCount;
                RequestValidationLoad(request.PersistenceLoad, "A");
                validationStage = 3;
                return;
            }
            if (validationStage == 3)
            {
                if (!validationCallback || ++validationFrames < 10) return;
                Check(!ReferenceEquals(validationPreviousPlayer, game.Player) && persistence.NativeWorldDisposalCount == 2 &&
                    !persistence.CombatRestorationPending && relationship.State == RelationshipState.Mounted &&
                    persistence.SemanticRestoreCount == validationSemantic + validationInitialActors &&
                    persistence.PresentationRestoreCount == validationPresentation + 1,
                    "P06-valid-native-retry-releases-fence-and-restores-actual-saved-participation");
                rider = relationship.Rider; mount = relationship.Mount;
                validationSemantic = persistence.SemanticRestoreCount;
                validationPresentation = persistence.PresentationRestoreCount;
                var riderDebt = MountedPersistenceService.CaptureActor(rider);
                var mountDebt = MountedPersistenceService.CaptureActor(mount);
                foreach (var actor in new[] { rider, mount, rider, mount })
                { persistence.RestoreActorAfterPostLoad(actor); invalidCombatDuplicateNotifications++; }
                persistence.Update(); persistence.Update(); controls.Update(); controls.Update();
                Check(persistence.SemanticRestoreCount == validationSemantic &&
                    persistence.PresentationRestoreCount == validationPresentation &&
                    LegitimateContinuation(riderDebt, MountedPersistenceService.CaptureActor(rider), 0) &&
                    LegitimateContinuation(mountDebt, MountedPersistenceService.CaptureActor(mount), 0) &&
                    controls.CaptureSnapshot().ExactFactCount == beforeControls.ExactFactCount &&
                    controls.CaptureSnapshot().DuplicateFactCount == 0 && controls.NativeCastRequestCount == 0,
                    "P07-repeated-real-actor-notifications-neither-refresh-debt-nor-duplicate-controls");
                VerifyValidationArchives();
                Write("validation-valid-retry", InvalidCombatDetail());
                validationContinuation = true; stage = 0;
            }
        }

        private void VerifyBlockedNativeCombat()
        {
            var game = Game.Instance;
            var round = game.TurnBasedCombatController.RoundNumber;
            var turn = game.TurnBasedCombatController.CurrentTurn;
            var sequence = combat.PairedActivationSequence;
            var commands = rider.Commands.Raw.Concat(rider.Commands.Queue).ToArray();
            var position = rider.Position;
            game.TurnBasedCombatController.Tick();
            var denied = new UnitMoveTo(position + Vector3.forward) { CreatedByPlayer = true };
            rider.Commands.Run(denied);
            invalidCombatNativeProbes++;
            Check(persistence.CombatRestorationPending && persistence.NativeCombatBlocksSave(game.Player) &&
                game.TurnBasedCombatController.RoundNumber == round &&
                ReferenceEquals(turn, game.TurnBasedCombatController.CurrentTurn) &&
                combat.PairedActivationSequence == sequence &&
                rider.Commands.Raw.Concat(rider.Commands.Queue).SequenceEqual(commands) && rider.Position == position &&
                LegitimateContinuation(validationFailedRider, MountedPersistenceService.CaptureActor(rider), 0) &&
                LegitimateContinuation(validationFailedMount, MountedPersistenceService.CaptureActor(mount), 0),
                "P06-native-tick-command-and-save-admission-retain-block-without-fresh-grant");
        }

        private JObject InvalidCombatDetail() => new JObject {
            ["case"] = request.PersistenceCase, ["blocked"] = persistence.CombatRestorationPending,
            ["feedback"] = persistence.Feedback, ["semantic"] = persistence.SemanticRestoreCount,
            ["presentation"] = persistence.PresentationRestoreCount,
            ["initialActorCount"] = validationInitialActors, ["nativeCallback"] = validationCallback,
            ["nativeAdmissionProbes"] = invalidCombatNativeProbes, ["duplicateNotifications"] = invalidCombatDuplicateNotifications,
            ["canceledCallback"] = validationCanceledCallback, ["nativeWorldDisposals"] = persistence.NativeWorldDisposalCount,
            ["sourceHash"] = request.PersistenceLoad.Sha256, ["variantHash"] = request.PersistenceAlternate.Sha256
        };
    }
}
