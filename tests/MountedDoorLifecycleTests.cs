using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Sprint 2 review corrections A and B and the nine-second timer regression, driven offline with controlled time.
    // These are OFFLINE lifecycle regressions: the clock values are simulated game seconds, not a native route.
    internal static class MountedDoorLifecycleTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("door settlement evaluates only after the delayed graph settles for two frames", SettlementDelayedThenSettled);
            runner.Run("door settlement deadline with a pending cut awards nothing even when a sample would be clear", SettlementDeadlineClearSampleUnresolved);
            runner.Run("door settlement deadline issues no recovery even when a candidate would be admitted", SettlementDeadlineCandidateUnresolved);
            runner.Run("door settlement normal path: clear footprint completes, blocked footprint recovers once", SettlementNormalPaths);
            runner.Run("door admission plan rejects an off-turn click before any replacement", AdmissionPlanOffTurnMutatesNothing);
            runner.Run("door admission plan replaces exactly once and retains the same door", AdmissionPlanReplacementAndRetention);
            runner.Run("door approach stays unstarted across the nine-second boundary and satisfies once at arrival (offline)", ApproachCrossesNineSecondsUnstarted);
            runner.Run("door approach retries after a finished move, bounds retries by progress and times out a genuine stall", ApproachRetryStallAndExhaustion);
        }

        private static void SettlementDelayedThenSettled()
        {
            var life = new MountedDoorSettlementLifecycle(3f);
            var step = MountedDoorSettlementStep.Waiting;
            // The cut still requires its update for forty frames (0.66 s), then settles.
            for (var frame = 1; frame <= 40; frame++) step = life.Observe(false, frame / 60d, frame);
            TestRunner.Equal(MountedDoorSettlementStep.Waiting, step, "An unsettled graph was evaluated early.");
            step = life.Observe(true, 41 / 60d, 41);
            TestRunner.Equal(MountedDoorSettlementStep.Waiting, step, "A single settled frame was treated as settled.");
            step = life.Observe(true, 42 / 60d, 42);
            TestRunner.Equal(MountedDoorSettlementStep.Evaluate, step, "Two consecutive settled frames did not evaluate.");
            var sampled = 0; var searched = 0;
            var verdict = MountedDoorSettlementLifecycle.Resolve(step, () => { sampled++; return new MountedFootprintSample(true, 8, 8, 8); },
                () => { searched++; return true; }, false, false, true);
            TestRunner.True(sampled == 1 && searched == 0 && verdict.CollisionSafe && verdict.Disposition == MountedDoorRecoveryDisposition.NotNeeded,
                "A settled clear footprint did not complete collision-safe without a search.");
            TestRunner.True(life.Settled && Math.Abs(life.ElapsedSeconds - 42 / 60d) < 1e-9, "The settlement time was not recorded at the settled frame.");
        }

        private static void SettlementDeadlineClearSampleUnresolved()
        {
            var life = new MountedDoorSettlementLifecycle(3f);
            var step = MountedDoorSettlementStep.Waiting;
            // The graph update stays queued past the deadline; a stale sample would read clear.
            for (var frame = 1; frame <= 200; frame++) { step = life.Observe(false, frame / 60d, frame); if (step != MountedDoorSettlementStep.Waiting) break; }
            TestRunner.Equal(MountedDoorSettlementStep.Unresolved, step, "The deadline did not end the settlement as unresolved.");
            TestRunner.True(life.ElapsedSeconds > 3d, "The deadline fired before its bound.");
            var sampled = 0; var searched = 0;
            var verdict = MountedDoorSettlementLifecycle.Resolve(step, () => { sampled++; return new MountedFootprintSample(true, 8, 8, 8); },
                () => { searched++; return true; }, false, false, true);
            TestRunner.True(sampled == 0 && searched == 0, "An unresolved settlement sampled or searched an unconfirmed graph.");
            TestRunner.True(!verdict.CollisionSafe && !verdict.MoveAuthorized && !verdict.FootprintSampled &&
                verdict.Disposition == MountedDoorRecoveryDisposition.Unresolved, "An unresolved settlement awarded a clear placement or a move.");
            TestRunner.Equal("unresolved", MountedDoorOccupancyPolicy.LegacyRecoveryWord(MountedDoorRecoveryDisposition.Unresolved), "The unresolved word changed.");
            // A later observation cannot reopen the settlement.
            TestRunner.Equal(MountedDoorSettlementStep.Unresolved, life.Observe(true, 4d, 300), "An unresolved settlement reopened.");
        }

        private static void SettlementDeadlineCandidateUnresolved()
        {
            var life = new MountedDoorSettlementLifecycle(3f);
            var step = MountedDoorSettlementStep.Waiting;
            // Settled once, then the cut moved again (RequiresUpdate true) and never settled before the deadline.
            step = life.Observe(true, 0.1d, 6);
            for (var frame = 7; frame <= 400; frame++) { step = life.Observe(false, frame / 60d, frame); if (step != MountedDoorSettlementStep.Waiting) break; }
            TestRunner.Equal(MountedDoorSettlementStep.Unresolved, step, "A settlement that lost its settled frame did not end unresolved at the deadline.");
            var searched = 0;
            var verdict = MountedDoorSettlementLifecycle.Resolve(step, () => new MountedFootprintSample(true, 8, 7, 7), () => { searched++; return true; }, false, false, true);
            TestRunner.True(searched == 0 && !verdict.MoveAuthorized && verdict.Disposition == MountedDoorRecoveryDisposition.Unresolved,
                "A would-be recovery candidate was admitted on an unconfirmed graph.");
            var threw = false;
            try { MountedDoorSettlementLifecycle.Resolve(MountedDoorSettlementStep.Waiting, () => new MountedFootprintSample(true, 8, 8, 8), () => true, false, false, true); }
            catch (InvalidOperationException) { threw = true; }
            TestRunner.True(threw, "A waiting settlement was resolved.");
        }

        private static void SettlementNormalPaths()
        {
            var life = new MountedDoorSettlementLifecycle(3f);
            life.Observe(true, 0.016d, 1);
            var step = life.Observe(true, 0.033d, 2);
            TestRunner.Equal(MountedDoorSettlementStep.Evaluate, step, "A promptly settled graph did not evaluate.");
            var blocked = MountedDoorSettlementLifecycle.Resolve(step, () => new MountedFootprintSample(true, 8, 7, 7), () => true, false, false, true);
            TestRunner.True(blocked.FootprintSampled && blocked.CandidateSearched && blocked.CandidateAdmitted && blocked.MoveAuthorized &&
                blocked.Disposition == MountedDoorRecoveryDisposition.Admitted, "A blocked footprint with an admitted candidate did not authorize one recovery.");
            var noCandidate = MountedDoorSettlementLifecycle.Resolve(step, () => new MountedFootprintSample(false, 8, 2, 2), () => false, false, false, true);
            TestRunner.True(!noCandidate.MoveAuthorized && noCandidate.Disposition == MountedDoorRecoveryDisposition.Unrecoverable, "A blocked footprint without a candidate was moved.");
            var offTurn = MountedDoorSettlementLifecycle.Resolve(step, () => new MountedFootprintSample(true, 8, 7, 7), () => true, true, false, true);
            TestRunner.True(!offTurn.MoveAuthorized && offTurn.Disposition == MountedDoorRecoveryDisposition.RefusedTurn, "An off-turn recovery was authorized.");
            var busy = MountedDoorSettlementLifecycle.Resolve(step, () => new MountedFootprintSample(true, 8, 7, 7), () => true, false, false, false);
            TestRunner.True(!busy.MoveAuthorized && busy.Disposition == MountedDoorRecoveryDisposition.RefusedMountBusy, "A recovery replaced the mount's own command.");
        }

        private static void AdmissionPlanOffTurnMutatesNothing()
        {
            // A pending recovery owns the Move slot; an off-turn click must leave it (and every other operation) untouched.
            var plan = MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.RejectMountBusy, true, false);
            TestRunner.Equal(MountedDoorAdmissionPlan.RejectWrongTurn, plan, "An off-turn click over a pending recovery was not rejected first.");
            TestRunner.True(!MountedDoorApproachPolicy.PlanMutates(plan), "A wrong-turn rejection mutates the existing operation.");
            foreach (var disposition in (MountedDoorAdmissionDisposition[])Enum.GetValues(typeof(MountedDoorAdmissionDisposition)))
            {
                var p = MountedDoorApproachPolicy.PlanAdmission(disposition, true, false);
                TestRunner.True(p == MountedDoorAdmissionPlan.RejectWrongTurn && !MountedDoorApproachPolicy.PlanMutates(p),
                    "An off-turn click reached a replacement for " + disposition + ".");
            }
            TestRunner.True(!MountedDoorApproachPolicy.PlanMutates(MountedDoorAdmissionPlan.Reject) && !MountedDoorApproachPolicy.PlanMutates(MountedDoorAdmissionPlan.RetainSameDoor),
                "A rejection or retention mutates the existing operation.");
        }

        private static void AdmissionPlanReplacementAndRetention()
        {
            TestRunner.Equal(MountedDoorAdmissionPlan.ReplaceRecovery, MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.RejectMountBusy, true, true),
                "A legal click over the pair's own recovery did not replace it.");
            TestRunner.Equal(MountedDoorAdmissionPlan.Reject, MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.RejectMountBusy, false, true),
                "A busy mount (not our recovery) was replaced.");
            TestRunner.Equal(MountedDoorAdmissionPlan.RetainSameDoor, MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.RetainActiveSameDoor, false, true),
                "A same-door repeat did not retain the accepted command.");
            TestRunner.Equal(MountedDoorAdmissionPlan.ReplaceActiveDoor, MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.ReplaceActiveDoor, false, true),
                "Another door did not replace the active approach.");
            TestRunner.Equal(MountedDoorAdmissionPlan.ReplaceMountMovement, MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.ReplaceMountMovement, false, true),
                "A real-time player move did not yield.");
            TestRunner.Equal(MountedDoorAdmissionPlan.Reject, MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.RejectOtherPairCommand, true, true),
                "A pair attack was replaced by a door click.");
            TestRunner.Equal(MountedDoorAdmissionPlan.Admit, MountedDoorApproachPolicy.PlanAdmission(MountedDoorAdmissionDisposition.Admit, false, true), "An idle pair was not admitted.");
        }

        private static MountedDoorApproachObservation Obs(double now, MountedDoorApproachMoveState move, float distance, float displacement, bool inRange = false, bool clock = true)
        {
            return new MountedDoorApproachObservation { NowSeconds = now, Move = move, DistanceToPoint = distance, DisplacementSinceLastTick = displacement,
                InRange = inRange, StallClockRuns = clock, ReapproachSample = 0.5f, MoveResult = "Success" };
        }

        private static void ApproachCrossesNineSecondsUnstarted()
        {
            // OFFLINE: simulated game seconds at 60 Hz. A 45 m approach at 3 m/s takes 15 s, well past the native
            // nine-second interrupt that applies to STARTED commands only.
            var life = new MountedDoorApproachLifecycle();
            var first = life.Tick(Obs(0d, MountedDoorApproachMoveState.None, 45f, 0f));
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, first, "The approach did not begin its delegated move.");
            TestRunner.True(!life.Satisfied && life.MoveStarts == 1, "The command was satisfied before any movement.");
            var satisfiedAt = -1d; var otherActions = 0;
            for (var tick = 1; tick <= 15 * 60 + 30; tick++)
            {
                var now = tick / 60d; var distance = (float)Math.Max(1.9d, 45d - 3d * now);
                var inRange = distance <= 2f;
                var action = life.Tick(Obs(now, MountedDoorApproachMoveState.Live, distance, 0.05f, inRange));
                if (action == MountedDoorApproachAction.Satisfied) { satisfiedAt = now; break; }
                if (action != MountedDoorApproachAction.Wait) otherActions++;
                if (now > 9d && now < 14d) TestRunner.True(!life.Satisfied && action == MountedDoorApproachAction.Wait,
                    "The approach was interrupted or satisfied at " + now.ToString("F2") + " s, past the nine-second boundary while still moving.");
            }
            TestRunner.True(satisfiedAt > 9d && satisfiedAt < 15d && life.Satisfied && otherActions == 0 && life.MoveStarts == 1 && life.ApproachesWithoutProgress == 0,
                "The approach did not complete exactly once at valid arrival after crossing nine seconds (satisfied at " + satisfiedAt + ").");
            TestRunner.True(life.ElapsedSeconds > 9f, "The recorded approach time did not cross nine seconds.");
            TestRunner.Equal(MountedDoorApproachAction.Satisfied, life.Tick(Obs(20d, MountedDoorApproachMoveState.None, 1f, 0f, true)), "A satisfied approach restarted.");
        }

        private static void ApproachRetryStallAndExhaustion()
        {
            var life = new MountedDoorApproachLifecycle();
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(Obs(0d, MountedDoorApproachMoveState.None, 10f, 0f)), "No first move.");
            // The move finished outside range at 6 m (progress): retry after the native cooldown, not before.
            TestRunner.Equal(MountedDoorApproachAction.Retry, life.Tick(Obs(2d, MountedDoorApproachMoveState.Finished, 6f, 0f)), "A finished move outside range was not retried.");
            TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(Obs(2.1d, MountedDoorApproachMoveState.None, 6f, 0f)), "The retry ignored the native cooldown.");
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(Obs(2.5d, MountedDoorApproachMoveState.None, 6f, 0f)), "The retry did not begin after the cooldown.");
            TestRunner.True(life.MoveStarts == 2 && life.ApproachesWithoutProgress == 0, "Progress was not credited.");
            // A genuine stall: the live move holds the mount still for more than four game seconds (real time).
            var action = MountedDoorApproachAction.Wait;
            for (var tick = 0; tick <= 5 * 60 && action == MountedDoorApproachAction.Wait; tick++) action = life.Tick(Obs(2.5d + tick / 60d, MountedDoorApproachMoveState.Live, 6f, 0f));
            TestRunner.Equal(MountedDoorApproachAction.Stalled, action, "A still mount under a live approach never stalled.");
            TestRunner.True(life.ApproachesWithoutProgress == 1, "A stall at the same distance counted as progress.");
            // Off-turn / paused frames never count: the clock does not run.
            var life2 = new MountedDoorApproachLifecycle();
            life2.Tick(Obs(0d, MountedDoorApproachMoveState.None, 10f, 0f));
            var stalled = false;
            for (var tick = 0; tick <= 20 * 60; tick++) if (life2.Tick(Obs(tick / 60d, MountedDoorApproachMoveState.Live, 10f, 0f, false, false)) == MountedDoorApproachAction.Stalled) stalled = true;
            TestRunner.True(!stalled, "Stillness without movement opportunity was charged as a stall.");
            // A tick gap (turn-based rounds) is re-sampled, never charged.
            var life3 = new MountedDoorApproachLifecycle();
            life3.Tick(Obs(0d, MountedDoorApproachMoveState.None, 10f, 0f));
            life3.Tick(Obs(0.02d, MountedDoorApproachMoveState.Live, 10f, 0f));
            TestRunner.Equal(MountedDoorApproachAction.Wait, life3.Tick(Obs(12d, MountedDoorApproachMoveState.Live, 10f, 0f)), "A whole round was charged as stillness.");
            TestRunner.True(life3.StalledSeconds == 0f, "The tick gap accumulated stall time.");
            // Exhaustion: three finished approaches without 0.25 m of progress refuse the click.
            var life4 = new MountedDoorApproachLifecycle();
            var now = 0d; var exhausted = false;
            for (var attempt = 0; attempt < 4 && !exhausted; attempt++)
            {
                TestRunner.Equal(MountedDoorApproachAction.BeginMove, life4.Tick(Obs(now, MountedDoorApproachMoveState.None, 7f, 0f)), "Retry " + attempt + " did not begin.");
                now += 1d;
                var end = life4.Tick(Obs(now, MountedDoorApproachMoveState.Finished, 7f, 0f));
                exhausted = end == MountedDoorApproachAction.Exhausted;
                now += 1d;
            }
            TestRunner.True(exhausted && life4.ExhaustedReason != null && life4.ApproachesWithoutProgress == 3, "Approaches without progress were not bounded.");
            TestRunner.Equal(MountedDoorApproachAction.Exhausted, life4.Tick(Obs(now, MountedDoorApproachMoveState.None, 7f, 0f)), "An exhausted approach restarted.");
        }
    }
}
