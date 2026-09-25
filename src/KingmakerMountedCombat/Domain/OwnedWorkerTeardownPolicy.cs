using System;
using System.Threading.Tasks;

namespace KingmakerMountedCombat.Domain
{
    // What a teardown request may do about an owned archive worker.
    internal enum OwnedWorkerTeardownVerdict
    {
        // Nothing is owned. Teardown may proceed; there is nothing to finalize.
        Clear,

        // Ownership was established and the worker (if any) has settled.
        // Teardown may proceed after finalizing the scope exactly once.
        Settled,

        // Teardown must not proceed: either the worker is still able to commit
        // after the bounded wait, or ownership could not be established at all.
        // Nothing has been released and nothing may be unpatched or cleaned up.
        Refused
    }

    // Pure decision for unload/disposal. Game-type free so it is tested as a
    // real unit. The rule it encodes: a bounded wait expiring is NOT evidence
    // that cleanup is safe, and "could not establish ownership" is NOT "no
    // worker" -- both refuse. Only a verified-absent worker, or one that has
    // actually settled, permits teardown.
    internal static class OwnedWorkerTeardownPolicy
    {
        internal static OwnedWorkerTeardownVerdict Decide(bool hasScope, bool established, Task worker,
            Func<Task, int, bool> waitForSettlement, int milliseconds)
        {
            if (!hasScope) return OwnedWorkerTeardownVerdict.Clear;
            if (!established) return OwnedWorkerTeardownVerdict.Refused;
            if (worker == null) return OwnedWorkerTeardownVerdict.Settled;
            if (worker.IsCompleted) return OwnedWorkerTeardownVerdict.Settled;
            if (waitForSettlement == null) return OwnedWorkerTeardownVerdict.Refused;
            return waitForSettlement(worker, milliseconds)
                ? OwnedWorkerTeardownVerdict.Settled
                : OwnedWorkerTeardownVerdict.Refused;
        }
    }
}
