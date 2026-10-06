using System;

namespace KingmakerMountedCombat.Integration
{
    // Chunk 6B increment 6B.2: the one diagnostics-only seam for proving charge-admission compensation.
    //
    // Compensation after the charge command enters the rider's queue cannot be proved by waiting for a real
    // engine fault, so the diagnostic fixture arms this seam, the admission path fires it immediately after
    // AddToQueueFirst, and the fixture then reads whether every native owner was resolved exactly.
    //
    // Why this is not an escape hatch. It grants nothing and permits nothing: the only thing an armed hook
    // can do is throw, which is strictly harder than the unarmed path. It is null in production and in every
    // shipped code path; nothing in Integration ever assigns it; and it cannot weaken a guard, a threshold,
    // an allowlist or an acceptance assertion, because a thrown fault can only drive the refusal path.
    // Validate-Source pins both halves of that: exactly one firing site in the admission path, and arming
    // only from Diagnostics.
    internal static class MountedChargeAdmissionFault
    {
        // Armed by the diagnostic charge fixture for exactly one admission, and cleared by it afterwards.
        internal static Action AfterQueue;
        // Diagnostics may only make cleanup harder. The retained owner and postconditions
        // remain authoritative; faults cannot report a step complete or grant resources.
        internal static Action<string> BeforeCleanupStep = null;
        internal static Action AfterLeaseAcquired = null;

        internal static void FireCleanup(string step) => BeforeCleanupStep?.Invoke(step);
        internal static void FireAfterLeaseAcquired()
        {
            var hook = AfterLeaseAcquired;
            AfterLeaseAcquired = null;
            hook?.Invoke();
        }

        internal static void FireAfterQueue()
        {
            var hook = AfterQueue;
            if (hook == null)
            {
                return;
            }

            // One shot: a seam that stayed armed could affect an unrelated later admission.
            AfterQueue = null;
            hook();
        }
    }
}
