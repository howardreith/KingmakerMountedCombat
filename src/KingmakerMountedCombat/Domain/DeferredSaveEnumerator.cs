using System;
using System.Collections;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // The native loading queue owns ordering. This one operation owns only its
    // pre-serialization wait; no mutable "last save" or second request queue.
    internal sealed class DeferredSaveEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> inner;
        private readonly Func<bool> ready;
        private readonly Func<double> elapsed;
        private readonly Action beginWait;
        private readonly Action tickWait;
        private readonly Action endWait;
        private readonly double maximumWait;
        private Action startSerialization;
        private double beganAt;
        private bool activated;
        private bool waitingScope;
        private bool disposed;

        internal bool Waiting { get; private set; }
        internal bool SerializationStarted { get; private set; }
        internal bool FailedBeforeSerialization { get; private set; }

        internal DeferredSaveEnumerator(IEnumerator<T> inner, Func<bool> ready, Func<double> elapsed,
            Action beginWait, Action tickWait, Action endWait, double maximumWait)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.ready = ready ?? throw new ArgumentNullException(nameof(ready));
            this.elapsed = elapsed ?? throw new ArgumentNullException(nameof(elapsed));
            this.beginWait = beginWait ?? throw new ArgumentNullException(nameof(beginWait));
            this.tickWait = tickWait ?? throw new ArgumentNullException(nameof(tickWait));
            this.endWait = endWait ?? throw new ArgumentNullException(nameof(endWait));
            if (double.IsNaN(maximumWait) || double.IsInfinity(maximumWait) || maximumWait <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumWait));
            this.maximumWait = maximumWait;
        }

        // Called when the native queue selects this exact enumerator, before it
        // displays its loading screen. Creation/queueing alone acquires nothing.
        internal void Activate(Action start)
        {
            if (activated || disposed) throw new InvalidOperationException("Save operation was already activated or disposed.");
            activated = true;
            startSerialization = start ?? throw new ArgumentNullException(nameof(start));
            try
            {
                Waiting = !ready();
                if (!Waiting) { StartSerialization(); return; }
                beganAt = elapsed();
                waitingScope = true;
                beginWait();
            }
            catch (Exception exception) { Fail(exception); throw; }
        }

        public bool MoveNext()
        {
            if (disposed) return false;
            try
            {
                if (!activated) throw new InvalidOperationException("Deferred save did not enter through its verified native loading owner.");
                if (Waiting)
                {
                    if (ready()) StartSerialization();
                    else
                    {
                        var duration = elapsed() - beganAt;
                        if (double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0 || duration > maximumWait)
                            throw new InvalidOperationException("Save was not written: the current native effect did not finish within the bounded wait.");
                        tickWait();
                        return true;
                    }
                }
                if (inner.MoveNext()) return true;
            }
            catch (Exception exception) { Fail(exception); throw; }
            Dispose();
            return false;
        }

        public T Current
        {
            get
            {
                if (!activated || disposed) throw new InvalidOperationException("Save operation is not active.");
                if (Waiting) return default(T);
                try { return inner.Current; }
                catch (Exception exception) { Fail(exception); throw; }
            }
        }
        object IEnumerator.Current => Current;
        public void Reset() => throw new NotSupportedException();

        private void StartSerialization()
        {
            // Restore a user's pause before enabling the native loading pause.
            // Any failure still disposes the not-yet-enumerated native iterator.
            ReleaseWait();
            Waiting = false;
            startSerialization();
            startSerialization = null;
            SerializationStarted = true;
        }

        private void ReleaseWait()
        {
            if (!waitingScope) return;
            waitingScope = false;
            endWait();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Waiting = false;
            startSerialization = null;
            Exception failure = null;
            try { inner.Dispose(); } catch (Exception exception) { failure = exception; }
            try { ReleaseWait(); }
            catch (Exception exception)
            {
                if (failure != null) throw new AggregateException(failure, exception);
                throw;
            }
            if (failure != null) throw failure;
        }

        private void Fail(Exception original)
        {
            try { Dispose(); }
            catch (Exception cleanup) { throw new AggregateException(original, cleanup); }
            // Recovery is permitted only after complete owned cleanup, before
            // native serialization. Disposal failures retain native failure flow.
            FailedBeforeSerialization = activated && !SerializationStarted;
        }
    }
}
