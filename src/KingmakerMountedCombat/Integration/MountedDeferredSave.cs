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
        internal int FailedSaveCount { get; private set; }

        private SaveWaitFault diagnosticWait;

        // The fixture owns one exact already-authorized native save request.
        // This does not authorize storage or synthesize gameplay/load state.
        internal IDisposable ArmOwnedSaveWait(SaveInfo save)
        {
            NativePersistenceIsolation.RequireOwnedWrite(save);
            if (diagnosticWait != null) throw new InvalidOperationException("An owned wait fault is already armed.");
            diagnosticWait = new SaveWaitFault(this, save);
            return diagnosticWait;
        }

        private sealed class SaveWaitFault : IDisposable
        {
            private readonly MountedPersistenceService owner;
            internal readonly SaveInfo Save;
            internal bool Active = true;
            private bool claimed;
            internal SaveWaitFault(MountedPersistenceService owner, SaveInfo save) { this.owner = owner; Save = save; }
            internal bool TryClaim(SaveInfo request)
            {
                if (!Active || claimed || !ReferenceEquals(Save, request)) return false;
                claimed = true;
                return true;
            }
            public void Dispose()
            {
                Active = false;
                if (ReferenceEquals(owner.diagnosticWait, this)) owner.diagnosticWait = null;
            }
        }

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
                !NativeSaveEffectBoundary.HasUnresolvedProjectiles() &&
                !NativeSaveEffectBoundary.HasUnresolvedAbilities();
        }

        private IEnumerator<object> DeferNativeSave(IEnumerator<object> routine, SaveWaitFault fault)
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
                return fault?.Active != true && SaveEffectsReady();
            };
            return new DeferredSaveEnumerator<object>(routine, ready, () => timer.Elapsed.TotalSeconds,
                () =>
                {
                    world = Game.Instance.Player;
                    DeferredSaveCount++;
                    NotifySaveStatus("Saving after current actions and projectiles finish; new actions are briefly held.");
                    tick();
                }, tick, () =>
                {
                    if (restorePause && ReferenceEquals(Game.Instance?.Player, world)) Game.Instance.IsPaused = true;
                    world = null;
                }, 30d);
        }

        internal void ReportFailedSave(Exception exception)
        {
            FailedSaveCount++;
            NotifySaveStatus("Save was not written; the previous complete save is unchanged. " + exception.Message);
        }

        private void NotifySaveStatus(string message)
        {
            Report(message);
            try { Kingmaker.PubSubSystem.EventBus.RaiseEvent<Kingmaker.PubSubSystem.IWarningNotificationUIHandler>(
                handler => handler.HandleWarning(message, true)); }
            catch (Exception display) { logger.Exception("Save failure notification could not be displayed", display); }
        }

        internal void ReportAbandonedSave(Exception exception) =>
            Report("Native operation was canceled; owned save/load cleanup reported: " + exception.Message);
    }
}
