using System;
using System.Collections;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class ScopedEnumeratorTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("save iterator owns scope only while enumerated", Lifetime);
            runner.Run("save iterator cancellation cleans exactly once", Cancellation);
            runner.Run("save iterator cleanup survives native enumeration and disposal failure", Failures);
            runner.Run("save iterator failed acquisition restores partial scope", AcquisitionFailure);
            runner.Run("save iterator concurrent instances do not share lifecycle state", Independent);
        }

        private static void Lifetime()
        {
            var begin = 0; var end = 0; var inner = new Probe();
            var iterator = new ScopedEnumerator<int>(inner, () => begin++, () => end++);
            TestRunner.Equal(0, begin, "Enumerator creation acquired live scope.");
            TestRunner.True(iterator.MoveNext(), "First element missing.");
            TestRunner.Equal(7, iterator.Current, "Element was changed.");
            TestRunner.Equal(1, begin, "Scope was not acquired exactly once.");
            TestRunner.True(!iterator.MoveNext(), "Unexpected extra element.");
            iterator.Dispose();
            TestRunner.Equal(1, end, "Completion did not restore exactly once.");
            TestRunner.Equal(1, inner.DisposeCount, "Native iterator disposed more than once.");
        }

        private static void Cancellation()
        {
            var end = 0; var unused = new Probe();
            var iterator = new ScopedEnumerator<int>(unused, () => { throw new Exception(); }, () => end++);
            iterator.Dispose();
            TestRunner.Equal(0, end, "Unstarted iterator ended a scope it never acquired.");
            TestRunner.Equal(1, unused.DisposeCount, "Unstarted inner was abandoned.");
            iterator = new ScopedEnumerator<int>(new Probe(), () => { }, () => end++);
            iterator.MoveNext(); iterator.Dispose(); iterator.Dispose();
            TestRunner.Equal(1, end, "Canceled scope restoration count.");
            TestRunner.True(!iterator.MoveNext(), "Disposed iterator resumed.");
        }

        private static void Failures()
        {
            foreach (var phase in new[] { "move", "current", "dispose" })
            {
                var end = 0;
                var inner = new Probe { FailPhase = phase, FailDispose = true };
                var iterator = new ScopedEnumerator<int>(inner, () => { }, () => end++);
                var failed = false;
                try
                {
                    iterator.MoveNext();
                    var value = iterator.Current;
                    iterator.Dispose();
                }
                catch (Exception) { failed = true; }
                iterator.Dispose();
                TestRunner.True(failed, "Native failure was swallowed: " + phase);
                TestRunner.Equal(1, end, "Native failure left the live save scope: " + phase);
                TestRunner.Equal(1, inner.DisposeCount, "Native dispose was retried: " + phase);
            }
        }

        private static void AcquisitionFailure()
        {
            var end = 0; var inner = new Probe(); var failed = false;
            var iterator = new ScopedEnumerator<int>(inner,
                () => { throw new InvalidOperationException("partial acquisition"); }, () => end++);
            try { iterator.MoveNext(); } catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed, "Failed acquisition was swallowed.");
            TestRunner.Equal(1, end, "Partially acquired scope was not restored.");
            TestRunner.Equal(0, inner.MoveCount, "Native serialization began after failed acquisition.");
        }

        private static void Independent()
        {
            var active = 0;
            var a = new ScopedEnumerator<int>(new Probe(), () => active++, () => active--);
            var b = new ScopedEnumerator<int>(new Probe(), () => active++, () => active--);
            a.MoveNext(); b.MoveNext(); a.Dispose();
            TestRunner.Equal(1, active, "One save disposed another save's scope.");
            b.Dispose();
            TestRunner.Equal(0, active, "Second save scope remained active.");
        }

        private sealed class Probe : IEnumerator<int>
        {
            public string FailPhase;
            public bool FailDispose;
            public int DisposeCount;
            public int MoveCount;
            public int Current
            {
                get { if (FailPhase == "current") throw new InvalidOperationException("current"); return 7; }
            }
            object IEnumerator.Current => Current;
            public bool MoveNext()
            {
                MoveCount++;
                if (FailPhase == "move") throw new InvalidOperationException("move");
                return MoveCount == 1;
            }
            public void Reset() { throw new NotSupportedException(); }
            public void Dispose()
            {
                DisposeCount++;
                if (FailDispose) throw new InvalidOperationException("dispose");
            }
        }
    }
}