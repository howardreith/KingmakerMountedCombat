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
        public bool Drained { get; private set; }
        public string Fault { get; private set; }
        public bool RemovalFaulted { get; private set; }
        public bool RemovalAttempted { get; private set; }
        public bool Outstanding => Started && !Drained;

        public void CaptureCreated(T fact)
        {
            if (!Started || fact == null || Fact != null)
            {
                Fault = "ambiguous-created-fact";
                throw new InvalidOperationException("Charge fact creation did not have one exact owner.");
            }
            Fact = fact;
        }

        public void Acquire(Func<T> add)
        {
            if (Started) throw new InvalidOperationException("Charge fact acquisition cannot be replayed.");
            Started = true;
            try
            {
                var returned = add();
                if (Fact == null || !ReferenceEquals(Fact, returned))
                    throw new InvalidOperationException("Native charge fact return differs from its observed creation.");
                Acquired = true;
            }
            catch (Exception error)
            {
                Fault = "acquisition-unconfirmed:" + error.GetType().Name;
                throw;
            }
        }

        public bool TryRemove(Action<T> remove, Func<T, bool> postcondition)
        {
            if (!Started || Drained) return true;
            if (Fact == null || RemovalFaulted) return false;
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
            Drained = Acquired && Fault == null && postcondition(Fact);
            return Drained;
        }
    }
}
