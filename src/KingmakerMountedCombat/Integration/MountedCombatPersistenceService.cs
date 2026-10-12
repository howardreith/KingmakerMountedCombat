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
        internal bool CombatRestorationPending => Enabled && (areaRestoreFailure != null || abandonedCombat.Blocks(Game.Instance?.Player) ||
            RestorableArchive != null && combatRestoreFailure != null ||
            SelectedCombat != null && restoreLoad != null && !combatRestored);
        internal bool LoadingWorld => restoreLoad != null && !restoreLoad.World.NativeCompleted;

        internal bool NativeCombatBlocksSave(Player player)
        {
            if (CombatRestorationPending) return true;
            if (!player.IsInCombat) return false;
            if (!Enabled || !settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || CombatRestorationPending ||
                (CombatController.IsInTurnBasedCombat() && !Game.Instance.TurnBasedCombatController.Initialized))
                return true;
            // The native queue now defers its selected save before pausing the
            // game clock. UI admission does not pretend that effects are settled.
            return false;
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
