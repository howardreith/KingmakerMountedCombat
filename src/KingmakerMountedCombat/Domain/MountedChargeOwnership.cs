using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedChargeOwnershipState
    {
        Active,
        TerminalPendingCleanup,
        FaultedCleanup,
        FullyDrained
    }

    // Lifetime owner of one exact charge. Each attempt uses the existing independent-step
    // compensation runner; an exception or an unobservable postcondition never releases Identity.
    // Native references live in Identity/the closures, never in persistence data.
    public sealed class MountedChargeOwnership
    {
        private readonly IList<MountedChargeCompensationStep> steps;
        private readonly IList<MountedChargePostcondition> postconditions;
        private bool draining;

        public MountedChargeOwnership(object identity,
            IEnumerable<MountedChargeCompensationStep> steps,
            IEnumerable<MountedChargePostcondition> postconditions)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.steps = new List<MountedChargeCompensationStep>(steps ?? throw new ArgumentNullException(nameof(steps)));
            this.postconditions = new List<MountedChargePostcondition>(postconditions ?? throw new ArgumentNullException(nameof(postconditions)));
            if (this.steps.Count == 0 || this.postconditions.Count == 0)
                throw new ArgumentException("A charge owner requires cleanup steps and observable postconditions.");
        }

        public object Identity { get; }
        public MountedChargeOwnershipState State { get; private set; }
        public MountedChargeCompensation LastAttempt { get; private set; }
        public int AttemptCount { get; private set; }
        public bool CleanupRequested => State != MountedChargeOwnershipState.Active;
        public bool Drained => State == MountedChargeOwnershipState.FullyDrained;

        public void Retire()
        {
            if (State == MountedChargeOwnershipState.Active) State = MountedChargeOwnershipState.TerminalPendingCleanup;
        }

        public bool TryDrain(string trigger)
        {
            if (Drained) return true;
            if (draining) return false;
            // Retire delivery BEFORE invoking native interruption, which can reenter callbacks.
            Retire();
            draining = true;
            try
            {
                LastAttempt = new MountedChargeCompensation(trigger, steps);
                AttemptCount++;
                LastAttempt.Run();
                LastAttempt.ConfirmPostconditions(postconditions);
                State = LastAttempt.Complete ? MountedChargeOwnershipState.FullyDrained : MountedChargeOwnershipState.FaultedCleanup;
                return Drained;
            }
            finally { draining = false; }
        }
    }
}
