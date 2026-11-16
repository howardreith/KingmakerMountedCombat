using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // The decisions the mounted lifecycle makes about one pair's cleanup: whether it is complete, what a dismount
    // result says while the pair's stick-movement cleanup is still retained, whether a release retries that cleanup
    // deliberately, and whether a detached owner may be settled. Disable, runtime-error cleanup, shutdown, owner reuse
    // and the removal of the Harmony guards all read these, so none of them can take a retained obligation for a
    // settled one.
    public static class MountedCleanupCompletionPolicy
    {
        // A cleanup that left nothing behind, whatever state the relationship is in.
        public static bool IsWithoutResidue(TransitionResult result) =>
            result != null && result.Succeeded && !result.MovementAuthorityResidual && !result.PresentationResidual;

        // A cleanup that is complete: nothing left behind, and the relationship unmounted.
        public static bool IsComplete(TransitionResult result, RelationshipState stateAfter) =>
            IsWithoutResidue(result) && stateAfter == RelationshipState.Unmounted;

        // A dismount result while the pair still retains stick-movement cleanup (an owned command not yet retired, or
        // its captured agent not yet removed): not succeeded, movement authority residual, and it says why. The
        // coordinator's own verdict is otherwise kept.
        public static TransitionResult WithRetainedStickMovement(TransitionResult result, int retainedObligations, string lastFailure)
        {
            if (result == null || retainedObligations <= 0) return result;
            var errors = new List<string>(result.Errors ?? new string[0])
            {
                "Mounted stick-movement cleanup is incomplete: " + retainedObligations + " retained obligation(s)" +
                    (string.IsNullOrEmpty(lastFailure) ? "." : "; last failure: " + lastFailure + ".")
            };
            return new TransitionResult(false, result.State, result.Trigger, errors, true, result.PresentationResidual);
        }

        // Whether a dismount retries retained stick-movement cleanup deliberately. The lifecycle's own boundaries do
        // (a disable, a shutdown, an area or view boundary, a voluntary dismount). The failure paths that can run
        // every frame do not: the retry of a failed cleanup, and cleanup after an update failure. Those stay within
        // the automatic limit.
        public static bool ReleaseRecoversExplicitly(CleanupTrigger trigger, bool automaticRetry) =>
            !automaticRetry && trigger != CleanupTrigger.Exception;

        // A detached owner is not settled, and its boundary is not reused, while any of its cleanup remains.
        public static bool OwnerRetainsCleanup(bool cleanupRetryRequired, bool preparedPairResidue, bool retainedStickMovementCleanup) =>
            cleanupRetryRequired || preparedPairResidue || retainedStickMovementCleanup;

        // What of that cleanup holds the pair's world (review of fe22563a). Retained stick-movement cleanup does not:
        // the boundary keeps the exact command, view and agent, and retries them by itself. An admission discard, an
        // area arrival and a world replacement may go on over it; the owner stays, and the pair stays dismounted.
        public static bool OwnerHoldsWorld(bool cleanupRetryRequired, bool preparedPairResidue) =>
            OwnerRetainsCleanup(cleanupRetryRequired, preparedPairResidue, false);

        // The refusal of a restoration whose pair is still held by its earlier owner's detached cleanup.
        public const string HeldByDetachedCleanupRefusal =
            "The pair's earlier stick-movement cleanup is still outstanding; the pair stays dismounted until that cleanup is established.";
    }
}
