using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.UI.SettingsUI;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        private readonly UnifiedMountedTurnCoordinator unifiedTurn;
        private bool combatRestored;
        private string combatRestoreFailure;
        private readonly NativeLoadFailureFence<Player> abandonedCombat = new NativeLoadFailureFence<Player>();

        // Commands and preparation stay blocked until the selected world's
        // semantic state is rebound. Presentation may wait for views separately.
        internal bool CombatRestorationPending => CombatSemanticRestorationPending || ArchiveSmallPresentationPending;
        private bool CombatSemanticRestorationPending => Enabled && (areaRestoreFailure != null || abandonedCombat.Blocks(Game.Instance?.Player) ||
            RestorableArchive != null && combatRestoreFailure != null ||
            SelectedCombat != null && restoreLoad != null && !combatRestored);
        internal bool LoadingWorld => restoreLoad != null && !restoreLoad.World.NativeCompleted;

        internal bool NativeCombatBlocksSave(Player player)
        {
            var held = CombatRestorationPending;
            ObserveSaveAdmissionHold(held, player);
            if (held) return true;
            if (!player.IsInCombat) return false;
            if (!Enabled || !settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || CombatRestorationPending ||
                (CombatController.IsInTurnBasedCombat() && !Game.Instance.TurnBasedCombatController.Initialized))
                return true;
            // The native queue now defers its selected save before pausing the
            // game clock. UI admission does not pretend that effects are settled.
            return false;
        }

        private bool saveAdmissionHoldObserved;
        internal int SaveAdmissionHoldCount { get; private set; }

        // Known issues S01 diagnostic. The native save gate (SaveManager.IsSaveAllowed) asks this service through its
        // transpiled combat term, also when a menu only decides whether to enable its Save button, so a refusal here can
        // stop a save before Game.SaveGame is ever called and before the request line can be written. One line when
        // this service starts refusing saving for its own restoration state and one when it stops; never per call, and
        // it changes no answer. Ordinary in-combat refusals are not reported here (the request line names them).
        private void ObserveSaveAdmissionHold(bool held, Player player)
        {
            if (held == saveAdmissionHoldObserved) return;
            saveAdmissionHoldObserved = held;
            if (held) SaveAdmissionHoldCount++;
            // The transition is reported even when its detail cannot be read.
            string detail = null;
            if (held)
                try { detail = SaveAdmissionHoldDetail(player); }
                catch (Exception error) { detail = "detail unavailable (" + error.GetType().Name + ")"; }
            try
            {
                logger.Info(held
                    ? "Mounted save admission held: this mod refuses saving until its restoration state settles: " + detail + "."
                    : "Mounted save admission hold ended: this mod no longer refuses saving.");
            }
            catch (Exception) { }
        }

        private string SaveAdmissionHoldDetail(Player player) =>
            "areaRestoreFailure=" + (areaRestoreFailure ?? "none") + "; abandonedCombat=" + abandonedCombat.Blocks(player) +
            "; combatRestoreFailure=" + (combatRestoreFailure ?? "none") +
            "; selectedCombatUnrestored=" + (SelectedCombat != null && restoreLoad != null && !combatRestored) +
            "; presentationPending=" + ArchiveSmallPresentationPending;

        // Save/load repair: the KMC term of the native combat save gate, named (null when KMC does not refuse).
        internal string CombatSaveBlockReason(Player player)
        {
            if (player == null) return "no world";
            if (CombatSemanticRestorationPending) return "mounted combat restoration is pending";
            if (ArchiveSmallPresentationPending) return "mounted presentation restoration is pending";
            if (!player.IsInCombat) return null;
            if (!Enabled) return "the mod is disabled";
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn || settings.EnablePairedCommandScheduler)
                return "the paired configuration does not support combat saving";
            if (CombatController.IsInTurnBasedCombat() && !Game.Instance.TurnBasedCombatController.Initialized)
                return "the native turn controller is not initialized";
            return null;
        }

        // Save/load repair: one bounded line per native save request (Game.SaveGame 06000CE4, before its IsSaveAllowed
        // refusal, which raises only a UI warning). Pure reads: IsSaveAllowed and the KMC gate change nothing.
        internal void ObserveSaveRequest(Kingmaker.EntitySystem.Persistence.SaveInfo save)
        {
            var game = Game.Instance;
            var player = game?.Player;
            if (game == null || player == null) return;
            var allowed = game.SaveManager.IsSaveAllowed();
            string nativeTerm = null;
            if (!allowed)
            {
                if (game.CurrentlyLoadedArea == null) nativeTerm = "no loaded area";
                else if (NativeCombatBlocksSave(player))
                    nativeTerm = (player.IsInCombat ? "combat (" : "KMC (") + (CombatSaveBlockReason(player) ?? "native") + ")";
                else if (player.GameOverReason.HasValue) nativeTerm = "game over";
                else if (game.IsModeActive(Kingmaker.GameModes.GameModeType.Dialog)) nativeTerm = "dialog";
                else if (game.IsModeActive(Kingmaker.GameModes.GameModeType.Cutscene)) nativeTerm = "cutscene";
                else if (player.GlobalMap?.CurrentEncounterData != null) nativeTerm = "random encounter";
                else nativeTerm = "another native term";
            }
            logger.Info("Mounted save request observed: type=" + (save?.Type.ToString() ?? "none") + "; name=" + (save?.Name ?? "none") +
                DescribeNativeSaveOutcome(game, save, allowed) +
                "; nativeAllowed=" + allowed + (nativeTerm != null ? "; refusedBy=" + nativeTerm : "") +
                "; kmcCombatGate=" + (CombatSaveBlockReason(player) ?? "open") + "; inCombat=" + player.IsInCombat +
                "; turnBased=" + CombatController.IsInTurnBasedCombat() + "; mode=" + game.CurrentMode +
                "; saveScope=" + HasActiveSaveScope + "; saveFence=" + HasSaveAdmissionFence + "; saveDraining=" + SaveDraining +
                "; restoreFailure=" + (areaRestoreFailure != null) + "; loading=" +
                (Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance?.IsLoadingInProcess ?? false) + ".");
        }

        // Known issues S01: the refusals that follow IsSaveAllowed in Game.SaveGame are silent in the native log, so a
        // request that printed nativeAllowed=True could still be refused. Pure reads of the same values, in the native
        // order (NativeSaveRequestPolicy); the queue state shows what a queued operation waits behind. Isolated: if
        // any of these reads fails, the request line is still written with the fields it always had.
        private static string DescribeNativeSaveOutcome(Game game, Kingmaker.EntitySystem.Persistence.SaveInfo save, bool allowed)
        {
            try
            {
                if (save == null) return "; nativeOutcome=unknown (no descriptor)";
                var settingsRoot = SettingsRoot.Instance;
                var onlyOneSave = settingsRoot != null && settingsRoot.OnlyOneSave.CurrentValue;
                var autosaveEnabled = settingsRoot == null || settingsRoot.AutosaveEnabled.CurrentValue;
                // Native IsIronmanSave is true for every descriptor until a one-save slot exists, so it is reported
                // only where native consults it.
                var oneSaveSlot = onlyOneSave && game.SaveManager.IsIronmanSave(save);
                var outcome = NativeSaveRequestPolicy.Classify(
                    save.Type == Kingmaker.EntitySystem.Persistence.SaveInfo.SaveType.ForImport, allowed, onlyOneSave, oneSaveSlot,
                    save.Type == Kingmaker.EntitySystem.Persistence.SaveInfo.SaveType.Auto,
                    save.Type == Kingmaker.EntitySystem.Persistence.SaveInfo.SaveType.IronMan, autosaveEnabled);
                var loader = Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance;
                return "; nativeOutcome=" + NativeSaveRequestPolicy.Describe(outcome) + "; onlyOneSave=" + onlyOneSave +
                    "; oneSaveSlot=" + (onlyOneSave ? oneSaveSlot.ToString() : "n/a") + "; autosaveEnabled=" + autosaveEnabled +
                    "; queuedBehind=" + (loader == null ? "none" : string.Join(",", loader.QueuedNames.ToArray()));
            }
            catch (Exception error) { return "; nativeOutcome=unavailable (" + error.GetType().Name + ")"; }
        }

        private UnitEntityData[] CombatActors() =>
            NativeCombatTurnPersistence.ReferencedActors(Game.Instance.TurnBasedCombatController)
                .Concat(unifiedTurn.PersistenceActors).Concat(WorldRelationships.SelectMany(r => new[] { r.Rider, r.Mount }))
                .Where(u => u != null).Distinct().ToArray();

        private SavedCombatData CaptureCombat(bool archive = false)
        {
            if (NativeCombatBlocksSave(Game.Instance.Player) || !SaveEffectsReady())
                throw new InvalidOperationException("Combat save has not reached the supported native command boundary.");
            var saved = NativeCombatTurnPersistence.Capture(Game.Instance.TurnBasedCombatController, CombatActors());
            if (archive) unifiedTurn.CaptureSharedPersistence(saved);
            else unifiedTurn.CapturePersistence(saved);
            return saved;
        }

        internal bool BeforeCombatStart()
        {
            // A second native Enable/Reset notification in the same load must not
            // erase the restored round and create a new roster.
            return !(combatRestored && restoreLoad != null && !restoreLoad.World.NativeCompleted &&
                SelectedCombat != null);
        }

        internal bool BeforeCombatTick()
        {
            if (!CombatRestorationPending) return true;
            TryRestoreCombat();
            return !CombatRestorationPending;
        }

        internal void TryRestoreCombat()
        {
            if (ArchiveNativeEnabled) { TryRestoreArchiveCombat(); return; }
            if (!CombatRestorationPending || combatRestoreFailure != null ||
                !NativeMountedSaveFormat.CanRestore(loaded) || loaded.Data.Combat == null || restoreLoad == null) return;
            var game = Game.Instance;
            var data = loaded.Data;
            if (game?.Player == null || game.Player.GameId != selectedCampaign ||
                !restoreLoad.World.TryBind(game.Player) ||
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe != data.AreaId) return;
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler ||
                data.Combat.TurnBased != SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue)
            {
                BlockCombatRestoration("Saved combat policy differs from the active paired/mode settings.");
                return;
            }
            var controller = game.TurnBasedCombatController;
            if (data.Combat.Actors.Any(a => !restoredActors.ContainsKey(a.Native.Id)))
            {
                if (restoreLoad.World.NativeCompleted && !Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess)
                    BlockCombatRestoration("A saved combat actor did not resolve during native entity restoration.");
                return;
            }
            if (data.Combat.TurnBased && !controller.Initialized)
            {
                // SaveManager.LoadRoutine finishes before the area-loading
                // queue initializes this controller. Keep admission blocked
                // while those native steps run; do not classify that gap as bad data.
                if (restoreLoad.World.NativeCompleted && !Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess)
                    BlockCombatRestoration("The native turn controller did not become available for the saved combat.");
                return;
            }
            if (!data.Combat.TurnBased && !game.Player.IsInCombat)
            {
                // The saved real-time combat was captured only while the party was in combat (CaptureCombat
                // requires Player.IsInCombat). The engine recomputes that non-serialized flag from the group
                // counters as the restored units re-enter combat during the native load; rebinding earlier let
                // the paired lifetime maintenance retire the rebound activation as an ended encounter
                // (preview.154 CM07-mount-load-rt). Wait for the flag exactly as the turn-based branch waits
                // for the initialised turn controller; after the load has completed, classify the gap exactly.
                if (restoreLoad.World.NativeCompleted && !Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess)
                    BlockCombatRestoration("The party did not re-enter combat for the saved real-time combat.");
                return;
            }
            var actors = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            foreach (var row in data.Combat.Actors)
            {
                var matches = game.State.Units.Where(u => u.UniqueId == row.Native.Id).ToArray();
                if (matches.Length != 1 || matches[0] != restoredActors[row.Native.Id])
                {
                    BlockCombatRestoration("A saved combat actor is absent or not unique.");
                    return;
                }
                if (matches[0].View?.AgentASP == null) return;
                actors.Add(row.Native.Id, matches[0]);
            }
            try
            {
                NativeCombatActorPersistence.RestoreReferences(data.Combat, actors);
                NativeCombatTurnPersistence.Restore(controller, data.Combat, actors);
                unifiedTurn.RestorePersistence(data.Combat, actors);
                if (!data.Mounted && data.Combat.Paired != null)
                {
                    var pair = data.Combat.Paired;
                    var riderId = pair.RiderId ?? pair.PendingSplitId;
                    relationship.RetainRestoredParticipation(riderId == null ? null : actors[riderId],
                        pair.MountId == null ? null : actors[pair.MountId]);
                }
                combatRestored = true;
                Report("Saved native combat roster and paired participation rebound without Prepare/End; round=" +
                    controller.RoundNumber + "; current=" + (controller.CurrentTurn?.Unit.UniqueId ?? "none") +
                    "; activation=" + (unifiedTurn.ActivationIdentity ?? "none") + ".");
            }
            catch (Exception exception)
            {
                BlockCombatRestoration("Combat rebind failed: " + exception.Message);
                throw;
            }
        }

        private void BlockCombatRestoration(string reason)
        {
            var message = "Mounted combat restoration is blocked. " + reason +
                " Existing expenditure and source data are retained; load a valid save to resume.";
            if (combatRestoreFailure == message) return;
            combatRestoreFailure = message;
            Report(message);
            try { Kingmaker.PubSubSystem.EventBus.RaiseEvent<Kingmaker.PubSubSystem.IWarningNotificationUIHandler>(
                handler => handler.HandleWarning(message, true)); }
            catch (Exception display) { logger.Exception("Combat restoration warning could not be displayed", display); }
        }

        private void FenceAbandonedCombat()
        {
            if (SelectedCombat == null) return;
            abandonedCombat.Hold(Game.Instance?.Player);
            BlockCombatRestoration("The selected combat restoration was interrupted before completion.");
        }

        private void BeginLoadHousekeeping()
        {
            if (!DrainWorldCharge("load world replacement"))
                throw new InvalidOperationException("Load refused: charge cleanup ownership is unresolved.");
            CancelAreaTransition();
            // A load replaces the world: retained mounted intent never crosses it (its pairs are already dismounted).
            retainedIntent = null; ClearPendingLoadedRetainedIntent(); ResetRetainedSettle();
            NativeSaveEffectBoundary.Clear();
            unifiedTurn.DiscardPersistenceWorld();
            var cleanupErrors = new List<Exception>();
            foreach (var owner in WorldRelationships)
                try
                {
                    owner.CancelSavedOwnershipImport();
                    if (!owner.GuardBoundary(CleanupTrigger.LoadRequested))
                        cleanupErrors.Add(new InvalidOperationException("Loaded world cleanup retained mounted references."));
                }
                catch (Exception error) { cleanupErrors.Add(error); }
            if (cleanupErrors.Count != 0) throw new AggregateException(cleanupErrors);
            mountedWorld?.DiscardAdmissionBindings();
            areaRestoreFailure = null;
            combatRestored = false;
            combatRestoreFailure = null;
            abandonedCombat.Clear();
        }
    }
}
