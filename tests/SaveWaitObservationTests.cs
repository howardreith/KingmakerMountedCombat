using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Incident 2026-11-22 (post-combat save and door lock): a deferred save must
    // name what held it and why it ended. These execute the production domain
    // types; the deferred wait itself is exercised by scripts/Test-DeferredSaveWait.ps1.
    internal static class SaveWaitObservationTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("save wait world policy keeps the same refusals and names each reason", WorldPolicy);
            runner.Run("save wait mode refusals name the menu, dialog or cutscene, never a world change", ModeMessages);
            runner.Run("save wait observation reports each blocker change once and one end", Transitions);
            runner.Run("save wait observation bounds its transition reports and counts the rest", Bounded);
            runner.Run("save wait observation contains failing descriptions", ContainedDescriptions);
            runner.Run("save wait observation retains no blocking object", NoRetention);
            runner.Run("an immediately settled save reports no wait", NoWait);
            runner.Run("deferred save timeout is identified and names its blocker", TimeoutIdentity);
            runner.Run("deferred save timeout message failure falls back without blocking retirement", TimeoutMessageFault);
            runner.Run("deferred save cancellation is neither a timeout nor serialization", CancellationIdentity);
        }

        private static void WorldPolicy()
        {
            TestRunner.Equal(SaveWaitWorldState.Unchanged, SaveWaitWorldPolicy.Decide(true, true, false, true), "Ordinary play refused.");
            TestRunner.Equal(SaveWaitWorldState.WorldReplaced, SaveWaitWorldPolicy.Decide(false, true, false, true), "Unbound world accepted.");
            TestRunner.Equal(SaveWaitWorldState.WorldReplaced, SaveWaitWorldPolicy.Decide(true, false, true, false), "Replaced world misclassified.");
            TestRunner.Equal(SaveWaitWorldState.LoadingScreen, SaveWaitWorldPolicy.Decide(true, true, true, false), "Loading screen misclassified.");
            TestRunner.Equal(SaveWaitWorldState.ModeChanged, SaveWaitWorldPolicy.Decide(true, true, false, false), "Mode change accepted.");
            TestRunner.Equal(SaveWaitOutcome.ModeChanged, SaveWaitWorldPolicy.Outcome(SaveWaitWorldState.ModeChanged), "Mode outcome lost.");
            TestRunner.Equal(30d, SaveWaitWorldPolicy.MaximumWaitSeconds, "The existing bounded wait changed.");
            Throws<ArgumentOutOfRangeException>(() => SaveWaitWorldPolicy.Outcome(SaveWaitWorldState.Unchanged),
                "An unchanged world was given a failure outcome.");
        }

        private static void ModeMessages()
        {
            var menu = SaveWaitWorldPolicy.FailureMessage(SaveWaitWorldState.ModeChanged, "EscMode", "Leinna's AcidSplash projectile");
            TestRunner.Equal("Save was not written: the game menu opened while it waited for Leinna's AcidSplash projectile.", menu, "Menu refusal text.");
            foreach (var mode in new[] { "EscMode", "FullScreenUi", "Dialog", "Cutscene", "Rest", "GlobalMap", "Kingdom", "Unheard" })
            {
                var text = SaveWaitWorldPolicy.FailureMessage(SaveWaitWorldState.ModeChanged, mode, null);
                TestRunner.True(text.IndexOf("native world changed", StringComparison.Ordinal) < 0 && text.EndsWith("current actions to finish."),
                    "Mode " + mode + " misnamed: " + text);
            }
            TestRunner.True(SaveWaitWorldPolicy.FailureMessage(SaveWaitWorldState.ModeChanged, "Unheard", "x").Contains("changed to Unheard"),
                "Unknown mode was not reported as is.");
            TestRunner.True(SaveWaitWorldPolicy.FailureMessage(SaveWaitWorldState.LoadingScreen, null, "x").Contains("a loading screen started"),
                "Loading screen misnamed.");
            TestRunner.Equal("Save was not written: Akasa's UnitAttack did not finish within 30 seconds.",
                SaveWaitWorldPolicy.TimeoutMessage(30d, "Akasa's UnitAttack"), "Timeout text.");
        }

        private static void Transitions()
        {
            var observation = new SaveWaitObservation(4, "Quick:Quicksave1", 10d);
            var first = observation.Observe(SaveWaitBlockerKind.Projectile, 7, () => "Leinna's AcidSplash projectile", () => "isHit=True", 10.5d);
            TestRunner.True(first != null && first.StartsWith("Mounted save wait: request=4; name=Quick:Quicksave1; t=0.50s; blocker=Projectile;") &&
                first.Contains("waitingFor=Leinna's AcidSplash projectile") && first.Contains("detail=[isHit=True]"), "First blocker line: " + first);
            TestRunner.Equal(null, observation.Observe(SaveWaitBlockerKind.Projectile, 7, () => "x", () => "y", 11d), "Unchanged blocker reported again.");
            TestRunner.Equal(null, observation.Observe(SaveWaitBlockerKind.None, 0, () => "x", () => "y", 11d), "A settled read was a transition.");
            TestRunner.Equal("Leinna's AcidSplash projectile", observation.WaitingFor, "A settled read replaced the last blocker.");
            var second = observation.Observe(SaveWaitBlockerKind.Projectile, 8, () => "another projectile", () => "isHit=False", 12d);
            TestRunner.True(second != null && second.Contains("change=2"), "Same kind, different object was not a transition.");
            var end = observation.End(SaveWaitOutcome.ModeChanged, "mode=EscMode", 26.25d);
            TestRunner.True(end != null && end.StartsWith("Mounted save wait ended: request=4;") && end.Contains("outcome=ModeChanged") &&
                end.Contains("reason=mode=EscMode") && end.Contains("elapsed=16.25s") && end.Contains("blockerChanges=2"), "End line: " + end);
            TestRunner.Equal(null, observation.End(SaveWaitOutcome.TimedOut, null, 40d), "A second end was reported.");
            TestRunner.Equal(null, observation.Observe(SaveWaitBlockerKind.Command, 9, () => "x", () => "y", 41d), "An ended wait reported a blocker.");
        }

        private static void Bounded()
        {
            var observation = new SaveWaitObservation(1, "Quick:Q", 0);
            var reported = 0;
            for (var i = 1; i <= SaveWaitObservation.MaximumTransitionReports + 20; i++)
                if (observation.Observe(SaveWaitBlockerKind.Command, i, () => "c", () => "d", i) != null) reported++;
            TestRunner.Equal(SaveWaitObservation.MaximumTransitionReports, reported, "Transition reports were not bounded.");
            var end = observation.End(SaveWaitOutcome.TimedOut, null, 31);
            TestRunner.True(end.Contains("blockerChanges=" + (SaveWaitObservation.MaximumTransitionReports + 20)) && end.Contains("unreported=20"),
                "Suppressed transitions were not counted: " + end);
        }

        private static void ContainedDescriptions()
        {
            var observation = new SaveWaitObservation(2, null, 0);
            var line = observation.Observe(SaveWaitBlockerKind.Ability, 3, () => throw new NullReferenceException(), () => null, 1);
            TestRunner.True(line.Contains("name=<unnamed>") && line.Contains("waitingFor=<description failed: NullReferenceException>") &&
                line.Contains("detail=[<none>]"), "A failing description escaped: " + line);
        }

        private static void NoRetention()
        {
            var observation = new SaveWaitObservation(3, "Quick:Q", 0);
            var weak = ObserveTransient(observation);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            TestRunner.True(!weak.IsAlive && observation.WaitingFor == "transient blocker", "The observation retained its blocking object.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference ObserveTransient(SaveWaitObservation observation)
        {
            var blocker = new object();
            observation.Observe(SaveWaitBlockerKind.Command, RuntimeHelpers.GetHashCode(blocker),
                () => blocker == null ? null : "transient blocker", () => blocker.GetType().Name, 1);
            return new WeakReference(blocker);
        }

        private static void NoWait()
        {
            var observation = new SaveWaitObservation(5, "Quick:Q", 0);
            TestRunner.Equal(null, observation.Observe(SaveWaitBlockerKind.None, 0, () => "x", () => "y", 0), "A settled read reported a wait.");
            TestRunner.Equal(null, observation.End(SaveWaitOutcome.Serialized, null, 0), "A wait that never held reported an end.");
            TestRunner.True(observation.Ended && !observation.HasBlocker, "Unobserved wait state.");
        }

        private static void TimeoutIdentity()
        {
            var now = 0d; var disposals = 0; var released = 0;
            var operation = new DeferredSaveEnumerator<object>(new Inner(() => disposals++), () => false, () => now, () => { }, () => { },
                () => released++, 30d, null, () => SaveWaitWorldPolicy.TimeoutMessage(30d, "Leinna's AcidSplash projectile"));
            operation.Activate(() => throw new InvalidOperationException("serialized"));
            now = 30.01d;
            var message = Throws<InvalidOperationException>(() => operation.MoveNext(), "Timeout did not refuse.").Message;
            TestRunner.Equal("Save was not written: Leinna's AcidSplash projectile did not finish within 30 seconds.", message, "Timeout text.");
            TestRunner.True(operation.TimedOut && operation.Disposed && operation.FailedBeforeSerialization && !operation.SerializationStarted &&
                disposals == 1 && released == 1, "Timeout identity or cleanup.");
        }

        private static void TimeoutMessageFault()
        {
            var now = 0d;
            var operation = new DeferredSaveEnumerator<object>(new Inner(() => { }), () => false, () => now, () => { }, () => { }, () => { }, 30d,
                null, () => throw new InvalidOperationException("description"));
            operation.Activate(() => { });
            now = 31d;
            var message = Throws<InvalidOperationException>(() => operation.MoveNext(), "Timeout did not refuse.").Message;
            TestRunner.Equal("Save was not written: the current native effect did not finish within the bounded wait.", message, "Fallback text.");
            TestRunner.True(operation.TimedOut && operation.FailedBeforeSerialization, "A failing description changed the timeout.");
        }

        private static void CancellationIdentity()
        {
            var operation = new DeferredSaveEnumerator<object>(new Inner(() => { }), () => false, () => 0, () => { }, () => { }, () => { }, 30d);
            operation.Activate(() => { });
            operation.Dispose();
            TestRunner.True(operation.Disposed && !operation.TimedOut && !operation.SerializationStarted && !operation.Waiting,
                "Cancellation was classified as a timeout or serialization.");
        }

        private static T Throws<T>(Action action, string message) where T : Exception
        {
            try { action(); }
            catch (T expected) { return expected; }
            throw new InvalidOperationException(message);
        }

        private sealed class Inner : IEnumerator<object>
        {
            private readonly Action disposed;
            internal Inner(Action disposed) { this.disposed = disposed; }
            public object Current => null;
            public bool MoveNext() => false;
            public void Reset() { throw new NotSupportedException(); }
            public void Dispose() => disposed();
        }
    }
}
