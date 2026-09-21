using System;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.PubSubSystem;
using Kingmaker.UI.SettingsUI;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPersistenceService
    {
        internal int RejectedLoadCount { get; private set; }

        internal bool CanLoadBeforeWorldReplacement(SaveInfo save)
        {
            string rejection;
            try
            {
                if (save == null || !save.HasFileOnDisk || save.Saver == null)
                    rejection = "The selected native archive is not available.";
                else
                {
                    // Inspect a clone; keep the UI descriptor's handle and the
                    // currently loaded world's semantic state out of this read.
                    using (var reader = save.Saver.Clone())
                        rejection = MountedLoadAdmissionPolicy.Rejection(NativeMountedSaveStorage.Read(reader),
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
