using System;

namespace KingmakerMountedCombat.Domain
{
    // The buff step's acquisition record, used by the existing charge cleanup ledger.
    // Capture precedes native activation. A throwing native callback is not replayable:
    // collection absence cannot prove its remaining consequences completed.
    public sealed class MountedChargeFactOwnership<T> where T : class
    {
        public T Fact { get; private set; }
        public bool Started { get; private set; }
        public bool Acquired { get; private set; }
        public bool Acquiring { get; private set; }
        public bool Drained { get; private set; }
        public string Fault { get; private set; }
        public bool RemovalFaulted { get; private set; }
        public bool RemovalAttempted { get; private set; }
        public bool IdentityUncertain { get; private set; }
        public bool Outstanding => Started && !Drained;

        public void CaptureCreated(T fact)
        {
            if (!Started || fact == null || Fact != null)
            {
                Fault = "ambiguous-created-fact";
                IdentityUncertain = true;
                throw new InvalidOperationException("Charge fact creation did not have one exact owner.");
            }
            Fact = fact;
        }

        public void Acquire(Func<T> add)
        {
            if (Started) throw new InvalidOperationException("Charge fact acquisition cannot be replayed.");
            Started = true;
            Acquiring = true;
            try
            {
                var returned = add();
                if (Fact == null || !ReferenceEquals(Fact, returned))
                {
                    IdentityUncertain = true;
                    throw new InvalidOperationException("Native charge fact return differs from its observed creation.");
                }
                Acquired = true;
            }
            catch (Exception error)
            {
                Fault = "acquisition-unconfirmed:" + error.GetType().Name;
                throw;
            }
            finally { Acquiring = false; }
        }

        // Native expiry and a parent's stored-child cleanup can enter removal before
        // our cleanup runner. Remember that attempt before its callbacks; absence
        // after a throwing callback must never authorize replay of those callbacks.
        public void ObserveNativeRemoval(T fact)
        {
            if (Fact == null || !ReferenceEquals(Fact, fact))
                throw new InvalidOperationException("Foreign fact cannot mark an owned removal.");
            RemovalAttempted = true;
        }

        public bool TryRemove(Action<T> remove, Func<T, bool> postcondition)
        {
            if (!Started || Drained) return true;
            if (Acquiring || Fact == null || IdentityUncertain) return false;
            if (!postcondition(Fact) && !RemovalAttempted)
            {
                RemovalAttempted = true;
                try { remove(Fact); }
                catch (Exception error)
                {
                    RemovalFaulted = true;
                    Fault = (Fault == null ? string.Empty : Fault + "|") + "removal-unconfirmed:" + error.GetType().Name;
                    throw;
                }
            }
            // A callback failure is retained history. It is not a licence to replay
            // removal, and not permanent debt once the exact owner's full native
            // postconditions have independently become true. Ambiguous identity
            // remains non-dischargeable above.
            Drained = postcondition(Fact);
            return Drained;
        }
    }
}
