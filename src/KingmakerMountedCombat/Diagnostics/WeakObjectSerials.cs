using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Diagnostics
{
    // Collision-free object labels for objects whose lifetimes may not overlap. The exact
    // observed API is RuntimeHelpers.GetHashCode; its hash is not a unique object identity.
    // F5 r27 observed a repeated hash across native loads. Address reuse is one possible
    // explanation, but correctness here does not depend on it. Each first-seen reference receives the next serial; a live
    // reference keeps its serial, and a new object never receives an earlier one. Only weak
    // references are held, so labelling never extends a native object's lifetime.
    internal sealed class WeakObjectSerials
    {
        internal const string Contract = "weak-reference-serial-v1";
        private readonly List<KeyValuePair<WeakReference, long>> entries = new List<KeyValuePair<WeakReference, long>>();
        private long next;

        internal WeakObjectSerials(int capacity)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
        }

        internal int Capacity { get; }
        internal int Assigned => entries.Count;
        internal int FaultCount { get; private set; }

        internal long Get(object value)
        {
            if (ReferenceEquals(value, null)) return 0;
            try
            {
                foreach (var entry in entries)
                    if (ReferenceEquals(entry.Key.Target, value)) return entry.Value;
                if (entries.Count == Capacity) throw new InvalidOperationException("Weak object serial bound exceeded.");
                var serial = ++next;
                entries.Add(new KeyValuePair<WeakReference, long>(new WeakReference(value), serial));
                return serial;
            }
            catch { FaultCount++; throw; }
        }
    }
}
