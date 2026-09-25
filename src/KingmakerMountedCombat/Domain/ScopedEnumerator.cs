using System;
using System.Collections;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // A resource belongs to enumeration, not to creation of an iterator.
    // Cleanup still runs when MoveNext, Current or inner Dispose throws.
    internal sealed class ScopedEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T> inner;
        private readonly Action begin;
        private readonly Action end;
        private bool started;
        private bool disposed;

        public ScopedEnumerator(IEnumerator<T> inner, Action begin, Action end)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            this.begin = begin ?? throw new ArgumentNullException(nameof(begin));
            this.end = end ?? throw new ArgumentNullException(nameof(end));
        }

        public T Current
        {
            get
            {
                if (!started || disposed) throw new InvalidOperationException("Enumerator is not active.");
                try { return inner.Current; }
                catch (Exception exception) { Fail(exception); throw; }
            }
        }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (disposed) return false;
            try
            {
                if (!started)
                {
                    // A partly acquired scope must also be released if begin fails.
                    started = true;
                    begin();
                }
                if (inner.MoveNext()) return true;
            }
            catch (Exception exception) { Fail(exception); throw; }
            Dispose();
            return false;
        }

        public void Reset() { throw new NotSupportedException(); }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Exception failure = null;
            try { inner.Dispose(); }
            catch (Exception exception) { failure = exception; }
            try { if (started) end(); }
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
        }
    }
}