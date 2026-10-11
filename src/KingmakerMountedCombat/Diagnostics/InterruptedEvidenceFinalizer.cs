using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // Evidence ordering only, never a native cleanup owner. A failed cleanup must not
    // prevent publication of the observed failed prefix. Publication failure remains
    // an exception and can be retried without inventing another interruption row.
    internal sealed class InterruptedEvidenceFinalizer
    {
        private bool recorded;
        internal bool Published { get; private set; }

        internal void Capture(Action recordFailure, Action cleanup, Action<Exception> recordCleanupFailure, Action publish)
        {
            if (Published) return;
            if (!recorded) { recordFailure(); recorded = true; }
            try { cleanup(); }
            catch (Exception exception) { recordCleanupFailure(exception); }
            publish();
            Published = true;
        }
    }
}
