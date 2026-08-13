using System;

namespace KingmakerMountedCombat.Diagnostics
{
    internal enum BoundaryFailureDrainState
    {
        Inactive,
        DrainingActiveLoad,
        ReadyToFinalize
    }

    /// <summary>
    /// Pure state machine that keeps a boundary failure pending until an active
    /// Kingmaker loading pipeline has stopped. The runtime engine owns the
    /// subsequent verification and cleanup; this type deliberately has no game
    /// or Unity dependency so the safety transition can be tested deterministically.
    /// </summary>
    internal sealed class BoundaryFailureDrain
    {
        public BoundaryFailureDrainState State { get; private set; }

        public string Failure { get; private set; }

        public void Request(string failure, bool loadingIsActive)
        {
            if (string.IsNullOrWhiteSpace(failure))
            {
                throw new ArgumentException("A boundary failure reason is required.", nameof(failure));
            }

            if (State != BoundaryFailureDrainState.Inactive)
            {
                return;
            }

            Failure = failure;
            State = loadingIsActive
                ? BoundaryFailureDrainState.DrainingActiveLoad
                : BoundaryFailureDrainState.ReadyToFinalize;
        }

        public BoundaryFailureDrainState Observe(bool loadingIsActive)
        {
            if (State == BoundaryFailureDrainState.DrainingActiveLoad && !loadingIsActive)
            {
                State = BoundaryFailureDrainState.ReadyToFinalize;
            }

            return State;
        }
    }
}
