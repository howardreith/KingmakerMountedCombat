using System;
using System.Runtime.CompilerServices;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    internal static class ObserverIdentityTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("native observer retains one exact reference for repeated labels", () => {
                using (var ids = new RetainedObjectIdentity(2, value => 17)) {
                    var command = new object();
                    TestRunner.Equal(0, ids.Get(null), "Null acquired an identity.");
                    TestRunner.Equal(17, ids.Get(command), "Native hash label changed.");
                    TestRunner.Equal(17, ids.Get(command), "Same reference changed identity.");
                    TestRunner.Equal(1, ids.RetainedCount, "Repeated observation acquired another object.");
                    TestRunner.Equal(0, ids.FaultCount, "Same reference was rejected.");
                }
            });
            runner.Run("native observer rejects equal hashes without losing the original owner", () => {
                using (var ids = new RetainedObjectIdentity(2, value => 17)) {
                    var first = new object(); ids.Get(first);
                    Reject(() => ids.Get(new object()));
                    TestRunner.Equal(17, ids.Get(first), "Collision discarded the original reference.");
                    TestRunner.Equal(1, ids.RetainedCount, "Collision replaced or added ownership.");
                    TestRunner.Equal(1, ids.FaultCount, "Collision was silently forgotten.");
                }
            });
            runner.Run("native observer compares references rather than value equality", () => {
                using (var ids = new RetainedObjectIdentity(2, value => 5)) {
                    ids.Get(new EqualValue()); Reject(() => ids.Get(new EqualValue()));
                    TestRunner.Equal(1, ids.FaultCount, "Equal values hid different native references.");
                }
            });
            runner.Run("native observer rejects a nonnull object with the null label", () => {
                using (var ids = new RetainedObjectIdentity(1, value => 0)) {
                    Reject(() => ids.Get(new object()));
                    TestRunner.Equal(0, ids.RetainedCount, "An invalid identity was retained as null.");
                    TestRunner.Equal(1, ids.FaultCount, "Null collision was unreported.");
                }
            });
            runner.Run("native identity capacity refuses new ownership while preserving earlier identities", () => {
                var first = new object();
                using (var ids = new RetainedObjectIdentity(1, value => ReferenceEquals(value, first) ? 1 : 2)) {
                    ids.Get(first); Reject(() => ids.Get(new object()));
                    TestRunner.Equal(1, ids.Get(first), "Capacity failure discarded an earlier object.");
                    TestRunner.Equal(1, ids.RetainedCount, "Identity bound was exceeded.");
                }
            });
            runner.Run("native objects survive collection until their observation is closed", () => {
                var ids = new RetainedObjectIdentity(2);
                var weak = ObserveTemporary(ids);
                Collect(); TestRunner.True(weak.IsAlive, "An observed object was released before trace closure.");
                var label = ids.Ids[0];
                ids.Dispose(); ids.Dispose(); Collect();
                TestRunner.True(!weak.IsAlive, "A closed trace retained a native reference.");
                TestRunner.Equal(0, ids.RetainedCount, "Dispose did not drain references.");
                TestRunner.Equal(label, ids.Ids[0], "Closure lost recorded identity evidence.");
                Reject(() => ids.Get(new object()));
            });
            runner.Run("native view replacement and null restoration keep separate argument and output identities", () => {
                var call = new ObservedViewAttachmentCall();
                var original = new object(); var replacement = new object(); var restored = new object();
                TestRunner.Equal(1, call.Begin(replacement, original), "First invocation changed.");
                TestRunner.Equal(1, call.Notify(replacement), "Replacement notification was lost.");
                TestRunner.Equal(1, call.End(replacement, replacement), "Replacement return was lost.");
                TestRunner.Equal(2, call.Begin(null, replacement), "Null argument did not start a distinct native call.");
                TestRunner.True(call.Argument == null, "Observer fabricated the native null argument.");
                TestRunner.Equal(2, call.Notify(restored), "Native-created restoration view was refused.");
                TestRunner.Equal(2, call.End(null, restored), "Null restoration did not finish.");
                TestRunner.Equal(0, call.Pending, "A completed native call remained pending.");
            });
            runner.Run("native view observer retains pending null calls on nested or unmatched entry", () => {
                var call = new ObservedViewAttachmentCall(); call.Begin(null, new object());
                Reject(() => call.Begin(new object(), new object()));
                Reject(() => call.End(null, new object()));
                TestRunner.Equal(1, call.Pending, "Failed observation silently cleared its pending call.");
            });
            runner.Run("native null view restoration requires an actually new observed view", () => {
                var view = new object(); var call = new ObservedViewAttachmentCall(); call.Begin(null, view);
                Reject(() => call.Notify(null)); Reject(() => call.Notify(view));
                TestRunner.Equal(1, call.Pending, "Failed restoration observation was dropped.");
            });
            runner.Run("native nonnull view replacement rejects an unrelated notification", () => {
                var call = new ObservedViewAttachmentCall(); call.Begin(new object(), new object());
                Reject(() => call.Notify(new object()));
            });
            runner.Run("native view observer rejects duplicate notifications and changed return arguments", () => {
                var call = new ObservedViewAttachmentCall(); var restored = new object();
                call.Begin(null, new object()); call.Notify(restored);
                Reject(() => call.Notify(restored)); Reject(() => call.End(restored, restored));
                Reject(() => call.End(null, new object()));
                TestRunner.Equal(1, call.End(null, restored), "Failed checks corrupted the original call identity.");
            });
            runner.Run("native view observer rejects orphan notification and return", () => {
                var call = new ObservedViewAttachmentCall();
                Reject(() => call.Notify(new object())); Reject(() => call.End(null, new object()));
                TestRunner.Equal(0, call.Pending, "Orphan callback fabricated a native call.");
            });
        }

        private static void Reject(Action action)
        {
            var rejected = false;
            try { action(); } catch (InvalidOperationException) { rejected = true; }
            TestRunner.True(rejected, "An invalid identity observation was accepted.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference ObserveTemporary(RetainedObjectIdentity ids)
        { var value = new object(); ids.Get(value); return new WeakReference(value); }
        private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        private sealed class EqualValue
        {
            public override bool Equals(object obj) => obj is EqualValue;
            public override int GetHashCode() => 5;
        }
    }
}
