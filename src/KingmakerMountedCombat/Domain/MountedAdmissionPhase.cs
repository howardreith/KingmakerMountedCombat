using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>
    /// The two admission boundaries a mounted transition passes, which are not the same
    /// question and must not share one answer.
    /// </summary>
    public enum MountedAdmissionPhase
    {
        /// <summary>
        /// Queue and target admission for a native Mount APPROACH. Everything that cannot
        /// change by moving must already hold; distance must not.
        /// </summary>
        Approach,

        /// <summary>
        /// Delivery-time admission for the relationship TRANSITION. Every mutable
        /// condition is revalidated here, adjacency included.
        /// </summary>
        Transition
    }

    /// <summary>
    /// One typed distinction between admitting a native approach and admitting the
    /// relationship transition.
    ///
    /// Requiring adjacency at prediction and target selection is circular: Kingmaker's own
    /// Move-typed <c>UnitUseAbility</c> is the thing that closes the distance, so refusing
    /// to create that command because the distance is not yet closed means the engine never
    /// gets the chance to approach at all. That is what blocked
    /// <c>CM02-approach-arrival</c> — "Mount from outside adjacency through legal native
    /// rider approach and legal arrival, with no teleport or manufactured endpoint" — and
    /// it cannot be fixed by moving actors together, widening the reach, enlarging the
    /// approach radius, or adding a scenario-only precursor move, because every one of
    /// those hides the product defect instead of repairing it.
    ///
    /// So distance is DEFERRED to the approach rather than waived. It is revalidated at
    /// delivery against live state, and nothing else is relaxed: every non-distance
    /// condition still blocks admission at the earlier boundary exactly as before.
    /// </summary>
    public static class MountedAdmissionPolicy
    {
        /// <summary>
        /// The only condition lawful to defer, stated once so a second one cannot be added
        /// by accident. Everything else must be satisfied before a command is created.
        /// </summary>
        public const string DeferredDistanceReason =
            "Not yet adjacent; the rider's own native Move approach closes the distance before the transition.";

        public static string DescribeDeferredDistance(string mountName)
        {
            var exactMountName = string.IsNullOrWhiteSpace(mountName) ? "mount" : mountName;
            return "Rider will approach " + exactMountName + " before mounting.";
        }

        /// <summary>
        /// Approach admission needs no blocking reasons. Transition admission additionally
        /// needs every deferred condition resolved.
        /// </summary>
        public static bool Admits(
            MountedAdmissionPhase phase,
            int blockingReasonCount,
            int transitionDeferredCount)
        {
            if (blockingReasonCount < 0 || transitionDeferredCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(blockingReasonCount));
            }
            if (blockingReasonCount != 0)
            {
                return false;
            }
            return phase == MountedAdmissionPhase.Approach || transitionDeferredCount == 0;
        }

        /// <summary>
        /// True only for the delivery boundary, where adjacency is a hard requirement.
        /// </summary>
        public static bool RequiresAdjacency(MountedAdmissionPhase phase)
        {
            return phase == MountedAdmissionPhase.Transition;
        }
    }
}
