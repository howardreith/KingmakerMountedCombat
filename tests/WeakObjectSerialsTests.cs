using System;
using System.Runtime.CompilerServices;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // F5 r27: subscriber labels must not depend on address-derived hash codes, which a new object
    // in a collected object's slot can repeat on Kingmaker's non-moving Boehm Mono runtime.
    internal static class WeakObjectSerialsTests
    {
        private sealed class AlwaysEqual
        {
            public override bool Equals(object obj) => obj is AlwaysEqual;
            public override int GetHashCode() => 7;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference LabelTransient(WeakObjectSerials serials, out long serial)
        {
            var value = new object();
            serial = serials.Get(value);
            return new WeakReference(value);
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); } catch (T) { return; }
            TestRunner.True(false, message);
        }

        internal static void Register(TestRunner runner)
        {
            runner.Run("weak serials: a live reference keeps its serial and distinct references get increasing ones", () =>
            {
                var serials = new WeakObjectSerials(8); var a = new object(); var b = new object();
                var first = serials.Get(a); var second = serials.Get(b);
                TestRunner.True(first == 1 && second == 2 && serials.Get(a) == first && serials.Get(b) == second && serials.Assigned == 2,
                    "Serials are not stable per live reference or not increasing.");
                TestRunner.True(serials.Get(null) == 0 && serials.Assigned == 2, "Null received a serial.");
            });
            runner.Run("weak serials: equal-but-distinct objects with one hash never share a serial", () =>
            {
                var serials = new WeakObjectSerials(8); var a = new AlwaysEqual(); var b = new AlwaysEqual();
                TestRunner.True(a.Equals(b) && a.GetHashCode() == b.GetHashCode(), "Fixture objects are not hash-colliding.");
                TestRunner.True(serials.Get(a) != serials.Get(b), "Hash-colliding distinct objects were merged.");
            });
            runner.Run("weak serials: labelling does not retain an object, and a later object never reuses its serial", () =>
            {
                var serials = new WeakObjectSerials(8);
                long old; var weak = LabelTransient(serials, out old);
                GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                TestRunner.True(!weak.IsAlive, "The serial registry kept a labelled object alive.");
                var replacement = new object();
                TestRunner.True(serials.Get(replacement) > old, "A new object received a collected object's serial.");
            });
            runner.Run("weak serials: the bound refuses and counts a fault", () =>
            {
                var serials = new WeakObjectSerials(1); var keep = new object(); serials.Get(keep);
                Throws<InvalidOperationException>(() => serials.Get(new object()), "Bound exceeded silently.");
                TestRunner.True(serials.FaultCount == 1 && serials.Get(keep) == 1, "Fault not counted or live serial changed.");
                Throws<ArgumentOutOfRangeException>(() => new WeakObjectSerials(0), "Zero capacity admitted.");
            });
        }
    }
}
