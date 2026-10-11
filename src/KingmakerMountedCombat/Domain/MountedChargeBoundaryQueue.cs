using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    internal enum MountedChargeBoundaryKind { Mode, PartyCombat, RiderRemoval, MountRemoval, DestroyedActor }

    // A deferred native notification, not charge state or a second cleanup owner.
    // The existing owner must drain before any notification may cross the boundary.
    internal sealed class MountedChargeBoundaryQueue
    {
        private sealed class Request
        {
            internal MountedChargeBoundaryKind Kind;
            internal object Identity;
            internal Func<bool> Current;
            internal Action Invoke;
            internal bool Attempted;
        }
        private readonly List<Request> requests = new List<Request>();
        private bool draining;
        internal bool Pending => requests.Count != 0;
        internal bool Contains(MountedChargeBoundaryKind kind, object identity) =>
            requests.Exists(request => request.Kind == kind && ReferenceEquals(request.Identity, identity));
        internal string Fault { get; private set; }
        internal string RetryError { get; private set; }
        internal int Completed { get; private set; }
        internal int Superseded { get; private set; }

        internal void Enqueue(MountedChargeBoundaryKind kind, object identity, Func<bool> current, Action invoke)
        {
            if (identity == null || current == null || invoke == null) throw new ArgumentNullException();
            var previous = requests.FindLast(r => r.Kind == kind && ReferenceEquals(r.Identity, identity) && !r.Attempted);
            if (previous != null)
            {
                requests.Remove(previous); Superseded++;
            }
            requests.Add(new Request { Kind = kind, Identity = identity, Current = current, Invoke = invoke });
        }

        internal bool TryDrain(Func<bool> ownershipSettled)
        {
            if (draining || Fault != null) return false;
            RetryError = null;
            draining = true;
            try
            {
                if (!ownershipSettled()) return false;
                // Native callbacks may append a successor. Leave it for a later update,
                // so a callback cannot create an unbounded loop on the game thread.
                var budget = requests.Count;
                while (requests.Count != 0 && budget-- > 0)
                {
                    if (!ownershipSettled()) return false;
                    var request = requests[0];
                    if (!request.Current()) { requests.RemoveAt(0); Superseded++; continue; }
                    request.Attempted = true;
                    request.Invoke();
                    requests.Remove(request); Completed++;
                }
                return !Pending;
            }
            catch (Exception exception)
            {
                // Native Reset/Enable/Disable may already have partially run. Never
                // retry their action/preparation effects after an ambiguous throw.
                var error = exception.GetType().Name + ": " + exception.Message;
                if (requests.Exists(request => request.Attempted)) Fault = error;
                else RetryError = error;
                return false;
            }
            finally { draining = false; }
        }
    }
}
