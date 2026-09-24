using System;
using UnityModManagerNet;

namespace KmcRemovalObserver
{
    // A separate minimal UMM mod. Without its own command-line request it does
    // nothing at all; with one it observes a prepared cleanup archive loading
    // and playing in a process where the KingmakerMountedCombat assembly is
    // genuinely absent, writes its result, and quits.
    public static class Main
    {
        private static RemovalObserverSession session;

        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            if (modEntry == null) return false;
            try
            {
                session = RemovalObserverSession.CreateFromCommandLine(modEntry);
                if (session == null)
                {
                    modEntry.Logger.Log("KMC removal observer " + ObserverIdentity.ProductVersion + " loaded inert: no observer request on the command line.");
                    return true;
                }
                modEntry.OnUpdate = OnUpdate;
                modEntry.Logger.Log("KMC removal observer " + ObserverIdentity.ProductVersion + " armed for run " + session.RunId + ".");
                return true;
            }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("Load", exception);
                RemovalObserverSession.TryReportBootstrapFailure(modEntry, exception);
                session = null;
                return false;
            }
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            var current = session;
            if (current == null) return;
            try { current.Update(); }
            catch (Exception exception)
            {
                modEntry.Logger.LogException("OnUpdate", exception);
                current.Fail("Observer update failed: " + exception.GetType().Name + ": " + exception.Message);
            }
        }
    }
}
