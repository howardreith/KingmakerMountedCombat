using System;
using System.Collections;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class DeferredSaveEnumeratorTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("deferred save stays outside serialization until native effects settle", Barrier);
            runner.Run("native queue activation isolates repeated save requests", NativeOrdering);
            runner.Run("abandoned waiting save restores pause and disposes once", Abandonment);
            runner.Run("deferred save timeout never enumerates a successful empty writer", Timeout);
            runner.Run("deferred save cleanup survives wait and native disposal failures", Failures);
            runner.Run("ready save retains native enumeration and completion order", Ready);
            runner.Run("unowned deferred enumeration fails before native serialization", Unowned);
        }

        private sealed class Writer : IEnumerator<object>
        {
            internal int Moves, Disposals;
            internal bool FailDispose;
            public object Current => "native";
            object IEnumerator.Current => Current;
            public bool MoveNext() => ++Moves == 1;
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { Disposals++; if (FailDispose) throw new InvalidOperationException("native disposal"); }
        }
        private static DeferredSaveEnumerator<object> Create(Writer writer, Func<bool> ready, Func<double> time,
            Action begin, Action tick, Action end) => new DeferredSaveEnumerator<object>(writer, ready, time, begin, tick, end, 30);

        private static void Barrier()
        {
            var writer = new Writer(); var settled = false; var events = new List<string>();
            var iterator = Create(writer, () => settled, () => 0, () => events.Add("resume"),
                () => events.Add("wait"), () => events.Add("restore-pause"));
            TestRunner.Equal(0, events.Count, "Queue creation acquired a scope.");
            iterator.Activate(() => events.Add("native-loading"));
            TestRunner.True(iterator.Waiting && !iterator.SerializationStarted, "Wait did not precede loading.");
            TestRunner.True(iterator.MoveNext() && iterator.Current == null, "Native wait yield was lost.");
            TestRunner.Equal(0, writer.Moves, "Writer enumerated while effects were unresolved.");
            settled = true;
            TestRunner.True(iterator.MoveNext() && !iterator.Waiting && iterator.SerializationStarted, "Safe barrier did not start writer.");
            TestRunner.Equal("resume,wait,restore-pause,native-loading", string.Join(",", events), "Pause/serialization ordering changed.");
            TestRunner.Equal("native", iterator.Current, "Native yield changed.");
            TestRunner.True(!iterator.MoveNext(), "Writer completion changed.");
            iterator.Dispose();
            TestRunner.Equal(1, writer.Disposals, "Writer disposed more than once.");
        }
        private static void NativeOrdering()
        {
            var settled = false; var starts = new List<int>();
            var aWriter = new Writer(); var bWriter = new Writer();
            var a = Create(aWriter, () => settled, () => 0, () => { }, () => { }, () => { });
            var b = Create(bWriter, () => settled, () => 0, () => { }, () => { }, () => { });
            var nativeQueue = new Queue<DeferredSaveEnumerator<object>>(); nativeQueue.Enqueue(a); nativeQueue.Enqueue(b);
            var first = nativeQueue.Dequeue(); first.Activate(() => starts.Add(1)); first.MoveNext();
            TestRunner.Equal(0, bWriter.Moves, "Queued save serialized before native selection.");
            TestRunner.True(!b.Waiting, "Queued save acquired the active operation's wait.");
            settled = true; first.MoveNext(); first.MoveNext();
            var second = nativeQueue.Dequeue(); second.Activate(() => starts.Add(2)); second.MoveNext(); second.MoveNext();
            TestRunner.Equal("1,2", string.Join(",", starts), "Repeated requests shared or replaced capture ownership.");
        }
        private static void Abandonment()
        {
            var writer = new Writer(); var restored = 0;
            var iterator = Create(writer, () => false, () => 0, () => { }, () => { }, () => restored++);
            iterator.Activate(() => { throw new Exception("abandoned save started"); });
            iterator.MoveNext(); iterator.Dispose(); iterator.Dispose();
            TestRunner.True(!iterator.Waiting && !iterator.MoveNext(), "Abandoned save retained admission gate.");
            TestRunner.Equal(1, restored, "Pause restore was not exactly once.");
            TestRunner.Equal(1, writer.Disposals, "Inner cancellation ownership changed.");
            TestRunner.Equal(0, writer.Moves, "Canceled wait serialized.");
        }
        private static void Timeout()
        {
            var writer = new Writer(); var now = 0d; var restored = 0;
            var iterator = Create(writer, () => false, () => now, () => { }, () => { }, () => restored++);
            iterator.Activate(() => { }); now = 31;
            var failed = false;
            try { iterator.MoveNext(); } catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed && !iterator.Waiting && !iterator.SerializationStarted, "Timeout reported normal completion.");
            TestRunner.Equal(0, writer.Moves, "Timed-out wait wrote native data.");
            TestRunner.Equal(1, restored, "Timeout stranded pause.");
        }
        private static void Failures()
        {
            foreach (var phase in new[] { "begin", "tick", "ready", "screen" })
            {
                var writer = new Writer { FailDispose = true }; var calls = 0; var restored = 0;
                var iterator = Create(writer, () => {
                    if (phase == "ready" && calls++ > 0) throw new Exception("changed world");
                    return phase == "screen";
                }, () => 0, () => { if (phase == "begin") throw new Exception("begin"); },
                    () => { if (phase == "tick") throw new Exception("tick"); }, () => restored++);
                var failed = false;
                try {
                    iterator.Activate(() => { if (phase == "screen") throw new Exception("screen"); });
                    iterator.MoveNext();
                } catch (AggregateException) { failed = true; }
                iterator.Dispose();
                TestRunner.True(failed && !iterator.Waiting, "Failure stranded gate: " + phase);
                TestRunner.Equal(phase == "screen" ? 0 : 1, restored, "Partial scope not released: " + phase);
                TestRunner.Equal(0, writer.Moves, "Failed pre-save boundary serialized: " + phase);
                TestRunner.Equal(1, writer.Disposals, "Failed dispose repeated: " + phase);
            }
        }
        private static void Ready()
        {
            var writer = new Writer(); var screen = 0;
            var iterator = Create(writer, () => true, () => 0, () => { throw new Exception(); },
                () => { throw new Exception(); }, () => { throw new Exception(); });
            iterator.Activate(() => screen++);
            TestRunner.Equal(0, writer.Moves, "Activation moved native snapshot boundary.");
            TestRunner.True(iterator.MoveNext() && !iterator.MoveNext(), "Native sequence altered.");
            TestRunner.Equal(1, screen, "Native loading screen started twice.");
        }
        private static void Unowned()
        {
            var writer = new Writer();
            var iterator = Create(writer, () => false, () => 0, () => { }, () => { }, () => { });
            var failed = false;
            try { iterator.MoveNext(); } catch (InvalidOperationException) { failed = true; }
            TestRunner.True(failed && writer.Moves == 0 && writer.Disposals == 1,
                "An unknown loading wrapper entered an indefinitely paused save.");
        }
    }
}
