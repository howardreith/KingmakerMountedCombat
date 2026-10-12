using System;
using System.Collections.Generic;
namespace KingmakerMountedCombat.Diagnostics
{
    // A terminal result is not permission to abandon a native worker or fixture
    // lease. The deadline records failure; only proven settlement opens the gate.
    internal sealed class Chunk7HostCompletionGate
    {
        internal const double MaximumDrainSeconds = 45;
        private readonly HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
        private double started;
        internal bool Pending { get; private set; }
        internal bool Settled { get; private set; }
        internal bool DeadlineExceeded { get; private set; }
        internal void Request(double now)
        {
            if (Pending || Settled) return;
            started = now; Pending = true;
        }
        internal bool Poll(double now, bool loading, Func<bool> workers, Func<bool> persistence,
            Func<bool> combat, Func<bool> world, Action<string> failure)
        {
            if (Settled) return true;
            if (!Pending) throw new InvalidOperationException("Chunk 7 completion drain was not requested.");
            if (!DeadlineExceeded && now - started > MaximumDrainSeconds)
            {
                DeadlineExceeded = true;
                failure("Chunk 7 cleanup exceeded its bounded drain. Native custody is retained; actor cleanup and process quit remain fenced.");
            }
            if (loading || !Attempt("native save worker/fence", workers, failure)) return false;
            // An independent owner is always polled even when its neighbour fails.
            var ownersSettled = Attempt("persistence fixture", persistence, failure);
            ownersSettled &= Attempt("combat fixture", combat, failure);
            if (!ownersSettled || !Attempt("all-pair world", world, failure)) return false;
            Settled = true; Pending = false; return true;
        }
        private bool Attempt(string owner, Func<bool> operation, Action<string> failure)
        {
            try { return operation(); }
            catch (Exception error)
            {
                var message = owner + " retains cleanup: " + error;
                if (reported.Add(message)) failure(message);
                return false;
            }
        }
    }
}
