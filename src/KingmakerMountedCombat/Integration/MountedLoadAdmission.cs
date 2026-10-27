using System;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.SettingsUI;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        internal int RejectedLoadCount { get; private set; }

        internal int NativeWorldDisposalCount { get; private set; }

        internal bool PrepareForNativeWorldDisposal()
        {
            if (Kingmaker.Game.Instance?.CurrentlyLoadedArea == null) return true;
            try
            {
                // LoadGameFromMainMenu destroys views before LoadRoutine is even
                // created. Release this world's leases while their native owners
                // still exist. DiscardPersistenceWorld does not End or forfeit.
                BeginLoadHousekeeping();
                NativeWorldDisposalCount++;
                return true;
            }
            catch (Exception exception)
            {
                logger.Exception("Mounted cleanup failed before native world disposal", exception);
                Report("Load canceled: the current mounted world could not release its owned references safely.");
                return false;
            }
        }


        internal bool CanLoadBeforeWorldReplacement(SaveInfo save)
        {
            string rejection;
            try
            {
                // A world replacement while an owned archive worker can still
                // commit would race that write. Refuse before disposal rather
                // than destroy the world the worker is still describing.
                if (SaveDraining || HasActiveSaveScope || combat.ChargeAdmissionFenced)
                    rejection = "a save is still being written; try again once it finishes.";
                else if (save == null || !save.HasFileOnDisk || save.Saver == null)
                    rejection = "The selected native archive is not available.";
                else
                {
                    // Inspect a clone; keep the UI descriptor's handle and the
                    // currently loaded world's semantic state out of this read.
                    // Admission reads with the same format SelectLoad will restore: the
                    // schema-1/2 reader reports every all-pair (schema-3) archive as Future,
                    // which refused KMC's own two-pair saves (observed natively in
                    // 20261018-c7b-world25-ownerseed-b3s2-load).
                    using (var reader = save.Saver.Clone())
                        rejection = ArchiveNativeEnabled
                            ? MountedLoadAdmissionPolicy.Rejection(NativeMountedSaveStorage.ReadArchive(reader),
                                save.GameId, save.Area?.AssetGuidThreadSafe, Enabled, settings.EnablePairedActivation,
                                settings.EnableUnifiedMountedTurn, settings.EnablePairedCommandScheduler,
                                SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue)
                            : MountedLoadAdmissionPolicy.Rejection(NativeMountedSaveStorage.Read(reader),
                                save.GameId, save.Area?.AssetGuidThreadSafe, Enabled, settings.EnablePairedActivation,
                                settings.EnableUnifiedMountedTurn, settings.EnablePairedCommandScheduler,
                                SettingsRoot.Instance.EnableTurnBasedMode.CurrentValue);
                }
            }
            catch (Exception exception)
            {
                logger.Exception("Mounted metadata admission failed before native world disposal", exception);
                rejection = "Mounted save metadata could not be read safely; the archive remains unchanged.";
            }
            if (rejection == null && !combat.TryDrainChargeOwnership("load admission"))
                rejection = "mounted charge cleanup must finish before world replacement.";
            if (rejection == null) return true;
            RejectedLoadCount++;
            var message = "Load canceled: " + rejection;
            Report(message);
            try { EventBus.RaiseEvent<IWarningNotificationUIHandler>(handler => handler.HandleWarning(message, true)); }
            catch (Exception exception) { logger.Exception("Mounted load warning display failed", exception); }
            return false;
        }
    }
}
