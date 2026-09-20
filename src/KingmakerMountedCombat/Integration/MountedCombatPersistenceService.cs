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

        // Commands and preparation stay blocked until the selected world's
        // semantic state is rebound. Presentation may wait for views separately.
        internal bool CombatRestorationPending => Enabled && loaded?.Data?.Combat != null &&
            restoreLoad != null && !combatRestored;
        internal bool LoadingWorld => restoreLoad != null && !restoreLoad.World.NativeCompleted;

        internal bool NativeCombatBlocksSave(Player player)
        {
            if (!player.IsInCombat) return false;
            if (!Enabled || !settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler || CombatRestorationPending ||
                !CombatController.IsInTurnBasedCombat() || !Game.Instance.TurnBasedCombatController.Initialized)
                return true;
            var actors = CombatActors();
            var unsettled = unifiedTurn.HasUnsettledPreparation || actors.Any(u => !u.Commands.Empty);
            if (unsettled) Report("Combat save is waiting for native commands to finish.");
            return unsettled;
        }

        private UnitEntityData[] CombatActors() =>
            NativeCombatTurnPersistence.ReferencedActors(Game.Instance.TurnBasedCombatController)
                .Concat(unifiedTurn.PersistenceActors).Concat(new[] { relationship.Rider, relationship.Mount })
                .Where(u => u != null).Distinct().ToArray();

        private SavedCombatData CaptureCombat()
        {
            if (NativeCombatBlocksSave(Game.Instance.Player))
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
            if (!CombatRestorationPending || combatRestoreFailure != null) return;
            var game = Game.Instance;
            var data = loaded.Data;
            if (game?.Player == null || game.Player.GameId != selectedCampaign ||
                !restoreLoad.World.TryBind(game.Player) ||
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe != data.AreaId) return;
            if (!settings.EnablePairedActivation || settings.EnableUnifiedMountedTurn ||
                settings.EnablePairedCommandScheduler ||
                data.Combat.TurnBased != SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue)
            {
                combatRestoreFailure = "Saved combat policy differs from the active paired/mode settings; restoration is blocked and metadata retained.";
                Report(combatRestoreFailure);
                return;
            }
            var controller = game.TurnBasedCombatController;
            if (!controller.Initialized || data.Combat.Actors.Any(a => !restoredActors.ContainsKey(a.Native.Id))) return;
            var actors = new Dictionary<string, UnitEntityData>(StringComparer.Ordinal);
            foreach (var row in data.Combat.Actors)
            {
                var matches = game.State.Units.Where(u => u.UniqueId == row.Native.Id).ToArray();
                if (matches.Length != 1 || matches[0] != restoredActors[row.Native.Id])
                {
                    combatRestoreFailure = "Saved combat actor is absent or not unique; participation restoration is blocked.";
                    Report(combatRestoreFailure);
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
                combatRestoreFailure = "Combat rebind failed; admission remains blocked and metadata retained: " + exception.Message;
                Report(combatRestoreFailure);
                throw;
            }
        }

        private void BeginLoadHousekeeping()
        {
            unifiedTurn.DiscardPersistenceWorld();
            if (!relationship.GuardBoundary(CleanupTrigger.LoadRequested))
                throw new InvalidOperationException("Loaded world cleanup retained mounted references.");
            combatRestored = false;
            combatRestoreFailure = null;
        }
    }
}
