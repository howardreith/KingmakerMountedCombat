using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // Only progress toward the required actor quotas may renew the fixture's
    // existing deadline. Extra rider routines cannot conceal a stalled mount.
    internal sealed class SustainedRoutineProgress
    {
        private readonly bool requireMount;
        private int observedProgress;

        internal SustainedRoutineProgress(bool requireMount)
        {
            this.requireMount = requireMount;
        }

        internal bool Observe(int completedRider, int completedMount)
        {
            if (completedRider < 0 || completedMount < 0)
                throw new ArgumentOutOfRangeException("Completed routine counts cannot be negative.");
            var progress = Math.Min(3, completedRider) + (requireMount ? Math.Min(3, completedMount) : 0);
            if (progress <= observedProgress) return false;
            observedProgress = progress;
            return true;
        }
    }
}
