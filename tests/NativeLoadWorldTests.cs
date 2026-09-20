using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    internal static class NativeLoadWorldTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("cold actor semantics precede late native campaign assignment", LateCampaign);
            runner.Run("queued load creation cannot bind the old world", Creation);
            runner.Run("canceled load cannot leak semantic or presentation ownership", Cancel);
            runner.Run("completed load rejects another same-campaign world", ReplacedWorld);
        }

        private sealed class World { internal string Campaign; }

        private static void LateCampaign()
        {
            var old = new World { Campaign = "selected" };
            var restored = new World();
            var boundary = new NativeLoadWorld<World>();
            boundary.Begin(old);
            TestRunner.True(!boundary.TryBind(old), "Old same-campaign world was accepted.");
            TestRunner.True(boundary.TryBind(restored), "Actor PostLoad was blocked by the unpublished campaign field.");
            TestRunner.True(!boundary.CanPresent(restored), "Presentation ran before native load completion.");
            restored.Campaign = "selected";
            boundary.Complete(restored);
            TestRunner.True(boundary.CanPresent(restored), "Completed native world could not present.");
            boundary.Close(); boundary.Close();
            TestRunner.True(!boundary.TryBind(restored), "Closed semantic scope reopened.");
        }

        private static void Creation()
        {
            var old = new World(); var next = new World();
            var boundary = new NativeLoadWorld<World>(); var began = false;
            using (var iterator = new ScopedEnumerator<int>(Empty(), () => { began = true; boundary.Begin(old); }, boundary.Close))
            {
                TestRunner.True(!began && !boundary.TryBind(next), "Creating an iterator acquired a world.");
            }
            TestRunner.True(!began, "Disposing an unstarted load began it.");
        }

        private static void Cancel()
        {
            var old = new World(); var next = new World();
            var boundary = new NativeLoadWorld<World>();
            using (var iterator = new ScopedEnumerator<int>(One(), () => boundary.Begin(old), boundary.Close))
            {
                iterator.MoveNext();
                TestRunner.True(boundary.TryBind(next), "Selected native load could not bind.");
            }
            TestRunner.True(!boundary.NativeCompleted && !boundary.TryBind(next) && !boundary.CanPresent(next),
                "Canceled native load retained restoration ownership.");
        }

        private static void ReplacedWorld()
        {
            var a = new World { Campaign = "same" }; var b = new World { Campaign = "same" };
            var boundary = new NativeLoadWorld<World>(); boundary.Begin(null); boundary.TryBind(a); boundary.Complete(a);
            TestRunner.True(!boundary.TryBind(b) && !boundary.CanPresent(b), "Another world inherited the selected save.");
            TestRunner.True(boundary.TryBind(a) && boundary.CanPresent(a), "Duplicate callback lost the selected world.");
            boundary.Close();
            var next = new NativeLoadWorld<World>(); next.Begin(a);
            TestRunner.True(next.TryBind(b) && !next.TryBind(a), "Next load could not isolate its world.");
        }

        private static IEnumerator<int> Empty() { yield break; }
        private static IEnumerator<int> One() { yield return 1; }
    }
}
