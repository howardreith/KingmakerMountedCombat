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
        internal bool CombatRestorationPending => Enabled && (abandonedCombat.Blocks(Game.Instance?.Player) ||
            loaded?.Data?.Combat != null && restoreLoad != null && !combatRestored);
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
                .Concat(unifiedTurn.PersistenceActors).Concat(new[] { relationship.Rider, relationship.Mount })
                .Where(u => u != null).Distinct().ToArray();

        private SavedCombatData CaptureCombat()
        {
            if (NativeCombatBlocksSave(Game.Instance.Player) || !SaveEffectsReady())
                throw new InvalidOperationException("Combat save has not reached the supported native command boundary.");
            var saved = NativeCombatTurnPersistence.Capture(Game.Instance.TurnBasedCombatController, CombatActors());
            unifiedTurn.CapturePersistence(saved);
            return saved;
        }

        internal bool BeforeCombatStart()
        {
            // A second native Enable/Reset notification in the same load must not
            // erase the restored round and create a new roster.
            return !(combatRestored && restoreLoad != null && !restoreLoad.World.NativeCompleted &&
                loaded?.Data?.Combat != null);
        }

        internal bool BeforeCombatTick()
        {
            if (!CombatRestorationPending) return true;
            TryRestoreCombat();
            return !CombatRestorationPending;
        }

        internal void TryRestoreCombat()
        {
            if (!CombatRestorationPending || combatRestoreFailure != null ||
                loaded?.Data?.Combat == null || restoreLoad == null) return;
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
                combatRestored = true;
                Report("Saved native combat roster and paired participation rebound without Prepare/End; round=" +
                    controller.RoundNumber + "; current=" + (controller.CurrentTurn?.Unit.UniqueId ?? "none") + ".");
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
            if (loaded?.Data?.Combat == null) return;
            abandonedCombat.Hold(Game.Instance?.Player);
            BlockCombatRestoration("The selected combat restoration was interrupted before completion.");
        }

        private void BeginLoadHousekeeping()
        {
            NativeSaveEffectBoundary.Clear();
            unifiedTurn.DiscardPersistenceWorld();
            if (!relationship.GuardBoundary(CleanupTrigger.LoadRequested))
                throw new InvalidOperationException("Loaded world cleanup retained mounted references.");
            combatRestored = false;
            combatRestoreFailure = null;
            abandonedCombat.Clear();
        }
    }
}
