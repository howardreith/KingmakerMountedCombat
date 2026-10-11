using System;
using System.Collections.Generic;
using System.Linq;

namespace KingmakerMountedCombat.Domain
{
    // A lease-local acquisition ledger. Pool return is a generation boundary:
    // a recycled object reference must never authorize touching its later user.
    public sealed class MountedChargeVisualOwnership<T> where T : class
    {
        public sealed class Acquisition
        {
            internal Acquisition(int generation, T value) { Generation = generation; Value = value; }
            public int Generation { get; }
            public T Value { get; }
            public bool Returned { get; internal set; }
            public bool CompletionRequested { get; internal set; }
            public string Failure { get; internal set; }
        }

        private readonly List<Acquisition> acquisitions = new List<Acquisition>();
        public IReadOnlyList<Acquisition> Acquisitions => acquisitions;

        public Acquisition Capture(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var current = acquisitions.LastOrDefault(item => ReferenceEquals(item.Value, value) && !item.Returned);
            if (current != null) return current;
            current = new Acquisition(acquisitions.Count + 1, value);
            acquisitions.Add(current);
            return current;
        }

        public bool ObserveReturn(T value, Func<T, bool> postcondition)
        {
            var current = acquisitions.LastOrDefault(item => ReferenceEquals(item.Value, value) && !item.Returned);
            if (current == null || !postcondition(value)) return false;
            current.Returned = true;
            return true;
        }

        // Native destruction may return to the pool before its caller clears a
        // reference. Preserve that caller's ordinary reference cleanup without
        // repeating destruction against the next user of the same object.
        public bool RequestCompletion(T value, Action<T> request)
        {
            var current = acquisitions.LastOrDefault(item => ReferenceEquals(item.Value, value));
            if (current == null) throw new InvalidOperationException("Unowned visual completion.");
            if (current.Returned || current.CompletionRequested) return false;
            current.CompletionRequested = true;
            try { request(value); }
            catch (Exception error) { current.Failure = error.GetType().Name; throw; }
            return true;
        }

        public bool TryComplete(Acquisition acquisition, Action<T> request, Func<T, bool> postcondition)
        {
            if (!acquisitions.Contains(acquisition)) throw new InvalidOperationException("Foreign visual acquisition.");
            if (acquisition.Returned) return true; // Never inspect or mutate a later pool user.
            if (postcondition(acquisition.Value)) { acquisition.Returned = true; return true; }
            if (!acquisition.CompletionRequested)
            {
                acquisition.CompletionRequested = true;
                try { request(acquisition.Value); }
                catch (Exception error) { acquisition.Failure = error.GetType().Name; }
            }
            if (!acquisition.Returned && postcondition(acquisition.Value)) acquisition.Returned = true;
            return acquisition.Returned;
        }

        public bool Drained => acquisitions.All(item => item.Returned);
    }
}
