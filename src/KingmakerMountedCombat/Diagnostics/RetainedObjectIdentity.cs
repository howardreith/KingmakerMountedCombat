using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace KingmakerMountedCombat.Diagnostics
{
    // A hash is only a label after its exact reference has been retained and checked.
    // Keep native objects alive for this bounded observation; never merge collisions.
    internal sealed class RetainedObjectIdentity : IDisposable
    {
        private readonly Dictionary<int, object> objects = new Dictionary<int, object>();
        private readonly Func<object, int> hash;
        private int[] releasedIds;

        internal RetainedObjectIdentity(int capacity, Func<object, int> hash = null)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            this.hash = hash ?? new Func<object, int>(RuntimeHelpers.GetHashCode);
        }

        internal int Capacity { get; }
        internal int FaultCount { get; private set; }
        internal int RetainedCount => objects.Count;
        internal bool Released => releasedIds != null;
        internal int[] Ids => Released ? (int[])releasedIds.Clone() : objects.Keys.OrderBy(id => id).ToArray();

        internal int Get(object value)
        {
            if (Released) throw new ObjectDisposedException(nameof(RetainedObjectIdentity));
            if (ReferenceEquals(value, null)) return 0;
            try
            {
                var id = hash(value);
                if (id == 0) throw new InvalidOperationException("A native object collided with the null identity.");
                object prior;
                if (objects.TryGetValue(id, out prior))
                {
                    if (!ReferenceEquals(prior, value))
                        throw new InvalidOperationException("Distinct native references have the same observed identity.");
                    return id;
                }
                if (objects.Count == Capacity) throw new InvalidOperationException("Native identity retention bound exceeded.");
                objects.Add(id, value);
                return id;
            }
            catch { FaultCount++; throw; }
        }

        public void Dispose()
        {
            if (Released) return;
            releasedIds = Ids;
            objects.Clear();
        }
    }
}
