using System;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Logging;
using UnityModManagerNet;

namespace KingmakerMountedCombat
{
    public static class Main
    {
        private static CompositionRoot root;
        private static UnityModManager.ModEntry activeModEntry;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            if (modEntry == null)
            {
                return false;
            }

            try
            {
                var logger = new UmmLogger(modEntry.Logger);
                activeModEntry = modEntry;
                root = new CompositionRoot(logger, modEntry.Info.Id);
                modEntry.OnToggle = OnToggle;
                modEntry.OnUnload = OnUnload;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnGUI = OnGui;
                modEntry.OnSessionStop = OnSessionStop;
                logger.Info("Kingmaker Mounted Combat " + BuildIdentity.ProductVersion + " loaded with transient private-alpha mounted melee services.");
                return true;
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("Load", exception);
                try
                {
                    RuntimeAutomationHost.TryReportBootstrapFailure(new UmmLogger(modEntry.Logger), modEntry.Info.Id, exception);
                }
                catch (Exception reportingException)
                {
                    modEntry.Logger.LogException("Runtime bootstrap failure reporting", reportingException);
                }
                root = null;
                activeModEntry = null;
                return false;
            }
        }

        internal static bool InvokeRegisteredToggleForAutomation(bool enabled)
        {
            var modEntry = activeModEntry;
            var callback = modEntry == null ? null : modEntry.OnToggle;
            if (modEntry == null || callback == null)
            {
                throw new InvalidOperationException("The exact registered UMM toggle callback is unavailable.");
            }

            return callback(modEntry, enabled);
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool enabled)
        {
            try
            {
                if (root == null)
                {
                    return false;
                }

                return root.SetEnabled(enabled);
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("OnToggle", exception);
                return false;
            }
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            try
            {
                root?.Dispose();
                root = null;
                activeModEntry = null;
                return true;
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("OnUnload", exception);
                return false;
            }
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            try
            {
                root?.Update(deltaTime);
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("OnUpdate", exception);
                try
                {
                    root?.HandleUpdateFailure(exception);
                }
                catch (Exception cleanupException)
                {
                    modEntry.Logger.LogException("OnUpdate cleanup", cleanupException);
                }
            }
        }

        private static void OnGui(UnityModManager.ModEntry modEntry)
        {
            try
            {
                root?.DrawGui();
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("OnGUI", exception);
            }
        }

        private static void OnSessionStop(UnityModManager.ModEntry modEntry)
        {
            try
            {
                if (root != null && !root.SetEnabled(false))
                {
                    modEntry.Logger.Error("OnSessionStop cleanup retained mounted residue; safety hooks remain installed.");
                }
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("OnSessionStop", exception);
            }
        }
    }
}
