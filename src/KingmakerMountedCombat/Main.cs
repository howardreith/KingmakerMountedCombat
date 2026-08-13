using System;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Logging;
using UnityModManagerNet;

namespace KingmakerMountedCombat
{
    public static class Main
    {
        private const string Version = "0.0.1-feasibility";
        private static CompositionRoot root;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            if (modEntry == null)
            {
                return false;
            }

            try
            {
                var logger = new UmmLogger(modEntry.Logger);
                root = new CompositionRoot(logger, modEntry.Info.Id);
                modEntry.OnToggle = OnToggle;
                modEntry.OnUnload = OnUnload;
                modEntry.OnUpdate = OnUpdate;
                modEntry.OnGUI = OnGui;
                modEntry.OnSessionStop = OnSessionStop;
                logger.Info("Kingmaker Mounted Combat " + Version + " loaded in diagnostic-only mode.");
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
                return false;
            }
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
                    if (root != null && !root.SetEnabled(false))
                    {
                        modEntry.Logger.Error("OnUpdate cleanup retained mounted residue; safety hooks remain installed.");
                    }
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
