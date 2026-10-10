using System;

namespace KingmakerMountedCombat.Integration
{
    // Chunk 6 closeout: the diagnostics-only seams for the two Chunk 6A boundaries no native path can reach.
    //
    // CM04-injected-exception: an exact exception at an owned boundary must fail closed with no residue. The
    // fixture arms BeforeMountExecution for exactly one native Mount execution; TryExecuteNativeMount fires it
    // after ledger admission and before the relationship transition, so the only thing an armed hook can reach
    // is the existing fail-closed path (Dismount(CleanupTrigger.Exception), no refund, feedback "failed closed").
    //
    // CM02-generation-change: a relationship generation change before delivery must refuse the stale shell. No
    // native path changes the generation while a shell is pending (Mount success and saved-pair restoration are
    // the only increments, and a concurrent Mount is suppressed), so the fixture arms BeforeShellGenerationCheck
    // for exactly one shell delivery resolution; ResolveDeliveringShell fires it immediately before its own
    // generation comparison, and the hook may only advance the counter through the relationship service's
    // diagnostic invalidation, which refuses outside an idle unmounted relationship.
    //
    // Why neither is an escape hatch: both are null in production and in every shipped code path, nothing in
    // Integration assigns them, each is consumed exactly once, and each can only make a delivery fail or be
    // refused. Neither can weaken a guard, grant a resource, or report a transition that did not happen.
    internal static class MountedRelationshipDeliveryFault
    {
        // Armed by the Chunk 6A diagnostic fixture for exactly one native Mount execution, consumed on firing.
        internal static Action BeforeMountExecution;

        // Armed by the Chunk 6A diagnostic fixture for exactly one shell delivery resolution, consumed on firing.
        // Returns the recorded diagnostic observation, or null when nothing was armed.
        internal static Func<string> BeforeShellGenerationCheck;

        internal static void FireBeforeMountExecution()
        {
            var hook = BeforeMountExecution;
            if (hook == null)
            {
                return;
            }

            // One shot: a seam that stayed armed could affect an unrelated later delivery.
            BeforeMountExecution = null;
            hook();
        }

        internal static string FireBeforeShellGenerationCheck()
        {
            var hook = BeforeShellGenerationCheck;
            if (hook == null)
            {
                return null;
            }

            BeforeShellGenerationCheck = null;
            return hook();
        }
    }
}
