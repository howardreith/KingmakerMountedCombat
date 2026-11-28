namespace KingmakerMountedCombat.Integration
{
    // PR #70 R12-F4 / R14-F5 (round 15): the ORDER of the services' teardown requests, extracted from CompositionRoot and
    // MountedLifecycleSubscriber so the production sequence can be executed offline. CompositionRoot.SetEnabled(false), its UpdateCore
    // and Dispose (UMM unload) and MountedLifecycleSubscriber.HandleModDisable delegate to these methods; the steps are thin adapters over
    // the same services they called before (persistence fences, MountedWorldService.SettleOutstandingPairCleanup, relationship
    // readiness, the lifecycle's cleanup, the Mounted Shield release, patches.Dispose). Every refusal happens before any hook or observer
    // is removed, and leaves the root, its per-frame drain and the outstanding obligations in place for a later request.
    internal static class MountedServiceTeardownSequence
    {
        internal interface ISteps
        {
            bool IsEnabled { get; }
            bool SaveSuspended { get; }
            bool LoadInFlight { get; }
            bool ActiveSaveWorkerRunning { get; }
            bool AreaTransitionPending { get; }
            bool CombatRestorationPending { get; }
            bool ChargeFenced { get; }
            bool TeardownOwnersReady { get; }
            // MountedWorldService.SettleOutstandingPairCleanup: one retry of every pair's outstanding owned cleanup; true when none remains.
            bool DrainOutstandingPairCleanup();
            // The bounded owned-archive-worker wait (MountedPersistenceService.DrainForTeardown): true when it refused.
            bool OwnedWorkerCanStillCommit();
            bool HandleModDisable();
            bool ReleaseForUnload();
            // Unpatching (patches.Dispose) and the release of every observer and service; reached only after every check passed.
            void RemoveHooksAndServices();
        }

        internal interface IModDisableSteps
        {
            bool TeardownOwnersReady { get; }
            bool AdmissionFenced { get; }
            bool DrainOutstandingPairCleanup();
            bool DrainChargeOwnership();
            // Ends paired participation, drops retained intent and runs the lifecycle's ModDisable cleanup (which cancels pair work).
            bool CleanupPairs();
        }

        internal enum Verdict { Ready, RefusedSaveOrOwners, RefusedLoad, RefusedResidue, RefusedOwnedWorker, RefusedContribution }

        // SetEnabled(false) up to and including HandleModDisable. Outstanding owned pair cleanup is drained once outside the save/load fences
        // (never while an owned archive worker serializes the live graphs or an owned load replaces the world) before the readiness check;
        // work it could not settle keeps teardown not ready and the request refused and retryable.
        internal static Verdict BeginDisable(ISteps steps)
        {
            if (!steps.SaveSuspended && !steps.LoadInFlight && !steps.ActiveSaveWorkerRunning) steps.DrainOutstandingPairCleanup();
            if (steps.SaveSuspended || steps.ChargeFenced || !steps.TeardownOwnersReady) return Verdict.RefusedSaveOrOwners;
            if (steps.LoadInFlight) return Verdict.RefusedLoad;
            return steps.HandleModDisable() ? Verdict.Ready : Verdict.RefusedResidue;
        }

        // UpdateCore's lifecycle-owned retry once ordinary pair updates have stopped (IsEnabled false after PerformFailureCleanup or a refused
        // disable): one bounded attempt per frame, never inside the save/load/area/restoration fences. True when an attempt was made.
        internal static bool RetryWhileDisabled(ISteps steps)
        {
            if (steps.IsEnabled || steps.ActiveSaveWorkerRunning || steps.SaveSuspended || steps.LoadInFlight || steps.AreaTransitionPending ||
                steps.CombatRestorationPending) return false;
            steps.DrainOutstandingPairCleanup();
            return true;
        }

        // Dispose (UMM unload). R14-F5: the owned-load fence comes first, so an unload during an owned load neither waits on, drains nor cancels
        // anything; then the bounded owned-worker wait; then HandleModDisable (its drains and cleanup) and the Mounted Shield release; hooks
        // and services are removed only after all of them succeeded.
        internal static Verdict Unload(ISteps steps)
        {
            if (steps.LoadInFlight) return Verdict.RefusedLoad;
            if (steps.OwnedWorkerCanStillCommit()) return Verdict.RefusedOwnedWorker;
            if (!steps.HandleModDisable()) return Verdict.RefusedResidue;
            if (!steps.ReleaseForUnload()) return Verdict.RefusedContribution;
            steps.RemoveHooksAndServices();
            return Verdict.Ready;
        }

        // MountedLifecycleSubscriber.HandleModDisable: one drain of outstanding owned cleanup first; anything still outstanding keeps teardown
        // not ready, so disable or unload is refused before any patch or observer is removed. The drain is repeated after the cleanup's own
        // cancellation (a carrier stop or target clear it created that failed) and the request is ready only when none remains. Both drain
        // verdicts are consumed.
        internal static bool ModDisable(IModDisableSteps steps)
        {
            var settledBefore = steps.DrainOutstandingPairCleanup();
            if (!settledBefore || !steps.TeardownOwnersReady || steps.AdmissionFenced) return false;
            if (!steps.DrainChargeOwnership()) return false;
            var cleaned = steps.CleanupPairs();
            var settledAfter = steps.DrainOutstandingPairCleanup();
            return cleaned && settledAfter && steps.TeardownOwnersReady;
        }
    }
}
