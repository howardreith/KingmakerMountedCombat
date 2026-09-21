using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.GameModes;
using Kingmaker.UnitLogic.Commands;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        internal int DeferredSaveCount { get; private set; }

        internal bool SaveEffectsReady()
        {
            var game = Game.Instance;
            if (game?.Player == null || game.State == null)
                throw new InvalidOperationException("Native save has no active world.");
            // Movement/approach intent is transient; its position and accrued
            // native debt are captured at the real header barrier. A running
            // action or undelivered projectile must first resolve natively.
            return !unifiedTurn.HasUnsettledPreparation &&
                !game.State.Units.Any(u => u.Commands.Raw.Concat(u.Commands.Queue)
                    .Any(NativeSaveEffectBoundary.CommandNeedsSettlement)) &&
                !NativeSaveEffectBoundary.HasUnresolvedProjectiles();
        }

        private IEnumerator<object> DeferNativeSave(IEnumerator<object> routine)
        {
            var timer = Stopwatch.StartNew();
            Player world = null;
            var restorePause = false;
            Action tick = () =>
            {
                var game = Game.Instance;
                if (world == null || !ReferenceEquals(game.Player, world) ||
                    LoadingProcess.Instance.IsManualLoadingScreenActive ||
                    (game.CurrentMode != GameModeType.Default && game.CurrentMode != GameModeType.Pause))
                    throw new InvalidOperationException("Save was not written: the native world changed while its effects were finishing.");
                if (game.IsPaused) { restorePause = true; game.IsPaused = false; }
            };
            Func<bool> ready = () =>
            {
                if (world != null && !ReferenceEquals(Game.Instance?.Player, world))
                    throw new InvalidOperationException("Save was not written: its native world was replaced.");
                return SaveEffectsReady();
            };
            return new DeferredSaveEnumerator<object>(routine, ready, () => timer.Elapsed.TotalSeconds,
                () =>
                {
                    world = Game.Instance.Player;
                    DeferredSaveCount++;
                    Report("Saving after current actions and projectiles finish; new actions are briefly held.");
                    tick();
                }, tick, () =>
                {
                    if (restorePause && ReferenceEquals(Game.Instance?.Player, world)) Game.Instance.IsPaused = true;
                    world = null;
                }, 30d);
        }

        internal void ReportAbandonedSave(Exception exception) =>
            Report("Native operation was canceled; owned save/load cleanup reported: " + exception.Message);
    }
}
