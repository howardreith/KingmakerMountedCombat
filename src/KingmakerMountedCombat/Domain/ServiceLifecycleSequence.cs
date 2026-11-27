using System;

namespace KingmakerMountedCombat.Domain
{
    // PR-4 round 15 (R12-F1): the external services the composition root's stop and start transitions act on. CompositionRoot implements it
    // over its live services (persistence, world, relationship, lifecycle, Mounted Shield, the Horse companion service and the rest); the
    // offline tests supply the engine/teardown side, so the production sequences below are executed exactly as the root runs them.
    internal interface IServiceLifecycleBoundary
    {
        bool IsEnabled { get; set; }
        bool FailureCleanupPending { set; }
        // Enable.
        bool EnableSelection();
        void StartRepair();
        void EnableServices();
        // Disable.
        bool SaveOrTeardownBusy { get; }
        bool LoadInFlight { get; }
        bool HandleModDisable();
        bool ShieldEnabled { get; }
        bool DisableShield(string reason);
        void EnableShield();
        bool DisableSelection();
        void StopRepair();
        void DisableServices();
        void Error(string message);
        // Failure cleanup: every pair's cleanup; returns the retained errors and how many were held only by stick movement.
        Exception[] VisitPairsForFailureCleanup(out int awaitingStickMovement);
        void SuspendShieldOffers();
        void ReleaseRetainedPendingReentry();
        void DropRetainedIntent(string reason);
    }

    // PR-4 round 15 (R12-F1): the composition root's successful-disable, enable and failure-cleanup transitions, moved here unchanged from
    // CompositionRoot.SetEnabled and CompositionRoot.PerformFailureCleanup, which now only delegate to them. Order, refusal and rollback
    // semantics are those of the root: a refused disable changes nothing (and restores Mounted Shield if it had been stopped), the repair
    // action is stopped only after a successful stop (IsEnabled cleared) and started only after a successful enable.
    internal static class ServiceLifecycleSequence
    {
        public static bool Enable(IServiceLifecycleBoundary b)
        {
            if (b.IsEnabled) { return true; }
            if (!b.EnableSelection())
            {
                b.Error("Mounted combat services could not enable the exact Ranger horse selection transaction.");
                return false;
            }
            b.IsEnabled = true;
            b.StartRepair();   // PR-4 round 14 (R12-F1): the repair action accepts intent again after re-enable
            b.EnableServices();
            return true;
        }

        public static bool Disable(IServiceLifecycleBoundary b)
        {
            // Disabling runs a full mounted cleanup over the exact live Player and cross-scene graphs an owned archive worker serializes on its
            // own thread. Nothing can cancel a started worker, so refuse the disable instead of mutating the write in flight; the save always
            // settles, and its drain keeps running because persistence.Update is deliberately not gated on IsEnabled.
            if (b.SaveOrTeardownBusy)
            {
                b.Error("Diagnostic services cannot be disabled while a mounted save is still being written; " +
                    "retry once it finishes.");
                return false;
            }
            // The same argument for the other live operation: cleanup during an owned world replacement would tear the pair down across a
            // world being replaced underneath it. Bounded by the load finishing.
            if (b.LoadInFlight)
            {
                b.Error("Diagnostic services cannot be disabled while a mounted save is still being loaded; " +
                    "retry once the area has finished loading.");
                return false;
            }
            // Always execute idempotent cleanup on a disable request. A prior update failure may already have cleared IsEnabled while a
            // partial runtime operation still needs best-effort cleanup.
            if (!b.HandleModDisable())
            {
                b.Error("Diagnostic services could not be disabled because mounted cleanup retained residue.");
                return false;
            }
            // Releases every Mounted Shield mount contribution (recorded and swept); the pairs are already dismounted. A contribution that
            // could not be removed keeps the services enabled, as other retained residue does. A refused disable below restores the service
            // exactly as it was before this call (enabled or not).
            var shieldWasEnabled = b.ShieldEnabled;
            if (!b.DisableShield("services disabled"))
            {
                b.Error("Diagnostic services could not be disabled because a Mounted Shield contribution remained on a mount.");
                return false;
            }
            if (!b.DisableSelection())
            {
                if (shieldWasEnabled) b.EnableShield();
                b.Error("Mounted combat services could not disable without overwriting an externally changed Ranger selection.");
                return false;
            }
            b.IsEnabled = false;
            b.StopRepair();   // PR-4 rounds 13-15 (R12-F1): no Horse Trip repair consent survives a stop
            b.DisableServices();
            return true;
        }

        public static void FailureCleanup(IServiceLifecycleBoundary b)
        {
            int awaitingStickMovement;
            var errors = b.VisitPairsForFailureCleanup(out awaitingStickMovement);
            // The Mounted Shield stop closes its selection and both benefits together and releases every contribution, verified. It never
            // runs while an owned load replaces the world (the outgoing world's stats are not touched).
            var shieldStopped = errors.Length == 0 && !b.LoadInFlight && b.DisableShield("update failure cleanup");
            if (errors.Length != 0 || !shieldStopped)
            {
                // Held back by retained stick-movement cleanup alone, or by a Mounted Shield stop that must wait (a contribution not yet
                // removed, or an owned load in progress), the stop is not abandoned: Update takes it again once that is settled.
                if (awaitingStickMovement == errors.Length) b.FailureCleanupPending = true;
                if (errors.Length != 0) throw new AggregateException("World update cleanup retained owners.", errors);
                throw new InvalidOperationException("Update-failure cleanup is waiting for the Mounted Shield stop (retained contribution or owned load); it is retried.");
            }
            // With its benefits stopped, Mounted Shield's offers close too (reopened by an explicit enable).
            b.SuspendShieldOffers();
            b.ReleaseRetainedPendingReentry();
            // Review finding 1: a failed runtime never re-attaches a pair it can no longer validate or synchronize.
            b.DropRetainedIntent("the mounted runtime stopped after an update failure");
            b.IsEnabled = false;
            b.StopRepair();   // PR-4 rounds 13-15 (R12-F1): no Horse Trip repair consent survives a stop
        }
    }
}
