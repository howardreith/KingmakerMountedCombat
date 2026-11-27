namespace KingmakerMountedCombat.Domain
{
    // The composition root's ordinary update-time write gate for live unit state (Mounted Shield's writes; the MMR-06
    // saved-Horse repair, review R2 R1-F2): services enabled, no save suspension, no area transition, no pending
    // combat/presentation restoration, no save worker serializing the live graphs, and no owned load in flight.
    public static class MountedWorldWriteGate
    {
        public static bool Allows(bool servicesEnabled, bool saveSuspended, bool areaTransitionPending, bool restorationPending,
            bool saveWorkerRunning, bool loadInFlight) =>
            servicesEnabled && !saveSuspended && !areaTransitionPending && !restorationPending && !saveWorkerRunning && !loadInFlight;
    }
}
