using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Sprint 3 (TRAP-01, SPELL-01): the offline decisions behind the mount's carried approach for the rider's own
    // native command. These are pure policy regressions; the engine seams (native OnTickApproaching, the delegated
    // move, the trap trigger-zone probe) are exercised natively.
    internal static class MountedCarriedApproachPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("carried approach: the mounted rider's own unstarted ability or trap interaction is carried", CarriesRiderAbilityAndTrap);
            runner.Run("carried approach: started, finished, unmounted, mount-executed and simulated commands are never carried", NeverCarriesForeignOrSettledCommands);
            runner.Run("carried approach: relationship shells, the charge shell and AI-originated commands stay native", NeverCarriesOwnedShellsOrAi);
            runner.Run("carried approach: a short move retries by the real-time lifecycle and signals the native end in turn-based combat", MoveEndSemanticsByMode);
            runner.Run("carried approach: replanning follows the recipient only beyond the epsilon and rejects non-finite drift", ReplanBounds);
            runner.Run("carried approach: a new rider request releases the carried command, its own request and other units do not", ReleaseOnRiderRequest);
            runner.Run("carried approach: the driver only replaces the native approach while it should approach (offline lifecycle)", LifecycleDrivesOnlyWhileApproaching);
        }

        private static MountedCarriedApproachDisposition Decide(
            bool mounted = true, bool rider = true, bool started = false, bool finished = false,
            MountedCarriedCommandKind kind = MountedCarriedCommandKind.Ability,
            bool shell = false, bool charge = false, bool ai = false, bool simulating = false)
        {
            return MountedCarriedApproachPolicy.Decide(mounted, rider, started, finished, kind, shell, charge, ai, simulating);
        }

        private static void CarriesRiderAbilityAndTrap()
        {
            TestRunner.Equal(MountedCarriedApproachDisposition.Carry, Decide(), "A rider ability was not carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.Carry, Decide(kind: MountedCarriedCommandKind.TrapInteraction), "A rider trap interaction was not carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(kind: MountedCarriedCommandKind.None), "An unrelated command was carried.");
        }

        private static void NeverCarriesForeignOrSettledCommands()
        {
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(started: true), "A started command was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(finished: true), "A finished command was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(mounted: false), "An unmounted rider's command was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(rider: false), "A command of another actor (the mount or a third unit) was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(simulating: true), "A cursor-simulated command was carried.");
        }

        private static void NeverCarriesOwnedShellsOrAi()
        {
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(shell: true), "A Mount/Dismount relationship shell was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(charge: true), "The mounted Charge shell was carried.");
            TestRunner.Equal(MountedCarriedApproachDisposition.NotCarried, Decide(ai: true), "An AI-originated cast was carried (general mounted AI is a later sprint).");
        }

        private static void MoveEndSemanticsByMode()
        {
            TestRunner.Equal(MountedCarriedMoveEndAction.LifecycleRetry, MountedCarriedApproachPolicy.OnDelegatedMoveEnded(false), "Real time did not retry by the lifecycle.");
            TestRunner.Equal(MountedCarriedMoveEndAction.SignalNativeApproachEnd, MountedCarriedApproachPolicy.OnDelegatedMoveEnded(true), "Turn-based combat did not defer to the native approach-end rule.");
        }

        private static void ReplanBounds()
        {
            var e = MountedCarriedApproachPolicy.ReplanEpsilonMetres;
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(0f, e), "A still recipient was replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(e, e), "Drift at the epsilon was replanned.");
            TestRunner.True(MountedCarriedApproachPolicy.ShouldReplan(e + 0.01f, e), "Drift beyond the epsilon was not replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(float.NaN, e), "NaN drift was replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(float.PositiveInfinity, e), "Infinite drift was replanned.");
            TestRunner.True(!MountedCarriedApproachPolicy.ShouldReplan(5f, -1f), "A negative epsilon replanned.");
        }

        private static void ReleaseOnRiderRequest()
        {
            TestRunner.True(MountedCarriedApproachPolicy.ReleasesOnRiderRequest(true, true, false), "A new rider command did not release the carried approach.");
            TestRunner.True(!MountedCarriedApproachPolicy.ReleasesOnRiderRequest(true, true, true), "The carried command's own (re)run released the approach.");
            TestRunner.True(!MountedCarriedApproachPolicy.ReleasesOnRiderRequest(true, false, false), "Another unit's command released the approach.");
            TestRunner.True(!MountedCarriedApproachPolicy.ReleasesOnRiderRequest(false, true, false), "A released approach was released again.");
        }

        // The driver reuses the accepted Sprint 2 lifecycle unchanged: one move while out of range, no move once in range.
        private static void LifecycleDrivesOnlyWhileApproaching()
        {
            var clock = new MountedDoorOperationalClock(); var life = new MountedDoorApproachLifecycle(); var frame = 1;
            Func<bool, MountedDoorApproachMoveState, float, MountedDoorApproachObservation> obs = (inRange, move, distance) => new MountedDoorApproachObservation
            {
                NowSeconds = clock.Observe(frame++, 1f / 60f), InRange = inRange, Move = move, DistanceToPoint = distance,
                DisplacementSinceLastTick = 0.1f, StallClockRuns = move == MountedDoorApproachMoveState.Live, ReapproachSample = 0.5f
            };
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(obs(false, MountedDoorApproachMoveState.None, 12f)), "Out of range did not begin the mount's move.");
            TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(obs(false, MountedDoorApproachMoveState.Live, 8f)), "A live move was not waited for.");
            TestRunner.Equal(MountedDoorApproachAction.Satisfied, life.Tick(obs(true, MountedDoorApproachMoveState.Live, 2f)), "In range did not satisfy.");
            TestRunner.Equal(MountedDoorApproachAction.Satisfied, life.Tick(obs(false, MountedDoorApproachMoveState.None, 2f)), "A satisfied lifecycle began another move.");
            TestRunner.Equal(1, life.MoveStarts, "More than one move was started for one in-range arrival.");
            TestRunner.True(!string.IsNullOrEmpty(MountedCarriedApproachPolicy.Feedback(MountedCarriedCommandKind.TrapInteraction, "Horse")) &&
                MountedCarriedApproachPolicy.ExhaustedFeedback(MountedCarriedCommandKind.Ability, "Horse", "no progress").Contains("cannot reach"),
                "Feedback strings are empty or lost the unreachable reason.");
        }
    }
}
