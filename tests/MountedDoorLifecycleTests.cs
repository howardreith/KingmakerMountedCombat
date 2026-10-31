using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Sprint 2 review corrections A and B and the nine-second timer regression, driven offline with controlled time.
    // These are OFFLINE lifecycle regressions: the clock values are simulated seconds, not a native route.
    // Review 2026-10-31 (turn-based clock): the four "operational clock" cases hold the campaign clock constant, as the
    // native turn-based controller does inside an acting turn, and drive the lifecycles through MountedDoorOperationalClock
    // with the engine's per-frame gameplay timestep. They are offline controlled-time evidence, not a claim of native time.
    internal static class MountedDoorLifecycleTests
    {
        private const float FrameSeconds = 1f / 60f;

        public static void Register(TestRunner runner)
        {
            runner.Run("door operational clock: frozen campaign time, the retry cooldown and the no-progress bound elapse on ticked frames", OperationalClockRetryUnderFrozenCampaignTime);
            runner.Run("door operational clock: a live move with opportunity and no displacement stalls under frozen campaign time", OperationalClockStallUnderFrozenCampaignTime);
            runner.Run("door operational clock: paused, off-turn and exhausted-movement frames charge nothing and resuming charges no gap", OperationalClockControlsChargeNothing);
            runner.Run("door operational clock: a pending navmesh update becomes Unresolved within its operational bound under frozen campaign time", OperationalClockSettlementUnresolvedUnderFrozenCampaignTime);
            runner.Run("door turn-based approach: an approach that ended on a turn (allocation spent) is re-attempted on a later turn only, never counted twice on the same turn", TurnBasedApproachEndDefersToLaterTurn);
            runner.Run("door settlement evaluates only after the delayed graph settles for two frames", SettlementDelayedThenSettled);
            runner.Run("door settlement deadline with a pending cut awards nothing even when a sample would be clear", SettlementDeadlineClearSampleUnresolved);
            runner.Run("door settlement deadline issues no recovery even when a candidate would be admitted", SettlementDeadlineCandidateUnresolved);
            runner.Run("door settlement normal path: clear footprint completes, blocked footprint recovers once", SettlementNormalPaths);
            runner.Run("door admission plan rejects an off-turn click before any replacement", AdmissionPlanOffTurnMutatesNothing);
            runner.Run("door admission plan replaces exactly once and retains the same door", AdmissionPlanReplacementAndRetention);
            runner.Run("door approach stays unstarted across the nine-second boundary and satisfies once at arrival (offline)", ApproachCrossesNineSecondsUnstarted);
            runner.Run("door approach retries after a finished move, bounds retries by progress and times out a genuine stall", ApproachRetryStallAndExhaustion);
        }

        // The campaign clock of a turn-based acting turn: rewound to the turn's start on every frame (constant).
        private const double FrozenCampaignSeconds = 100d;

        private static MountedDoorApproachObservation Tick(MountedDoorOperationalClock clock, ref int frame, MountedDoorApproachMoveState move,
            float distance, float displacement, bool clockRuns = true, float gameplayDelta = FrameSeconds)
        {
            var now = clock.Observe(frame++, gameplayDelta);
            return Obs(now, move, distance, displacement, false, clockRuns);
        }

        private static void OperationalClockRetryUnderFrozenCampaignTime()
        {
            // OFFLINE: campaign time stays at 100 s throughout (never read by the lifecycle); the operational clock advances by
            // the gameplay timestep on each ticked frame, so the native 0.2-0.6 s re-approach cooldown and the no-progress
            // bound both elapse. With campaign time as the basis (the former defect) the retry scheduled at 100.4 s never came.
            var clock = new MountedDoorOperationalClock(); var life = new MountedDoorApproachLifecycle(); var frame = 1;
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.None, 10f, 0f)), "No first move.");
            TestRunner.Equal(MountedDoorApproachAction.Retry, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Finished, 6f, 0f)), "A finished move outside range was not retried.");
            var retryAfterFrames = 0;
            for (var i = 1; i <= 120 && retryAfterFrames == 0; i++)
            {
                var action = life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.None, 6f, 0f));
                if (action == MountedDoorApproachAction.BeginMove) retryAfterFrames = i;
                else TestRunner.Equal(MountedDoorApproachAction.Wait, action, "The cooldown produced another action.");
            }
            // ReapproachSample 0.5 -> 0.4 s cooldown = 24 frames of 1/60 s; the bound admits the native 0.2-0.6 s range.
            TestRunner.True(retryAfterFrames >= 12 && retryAfterFrames <= 37, "The retry did not begin after the native cooldown measured in operational seconds (frames: " + retryAfterFrames + ").");
            TestRunner.True(life.MoveStarts == 2 && life.ApproachesWithoutProgress == 0 && Math.Abs(FrozenCampaignSeconds - 100d) < 1e-9, "Progress was not credited or campaign time moved.");
            // Genuine no-progress retries stay bounded under the same frozen campaign time.
            var exhausted = false;
            for (var attempt = 0; attempt < 4 && !exhausted; attempt++)
            {
                exhausted = life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Finished, 6f, 0f)) == MountedDoorApproachAction.Exhausted;
                if (exhausted) break;
                var action = MountedDoorApproachAction.Wait;
                for (var i = 0; i < 120 && action == MountedDoorApproachAction.Wait; i++) action = life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.None, 6f, 0f));
                TestRunner.Equal(MountedDoorApproachAction.BeginMove, action, "Retry " + attempt + " never began on ticked frames.");
            }
            TestRunner.True(exhausted && life.ExhaustedReason != null && life.ApproachesWithoutProgress == 3, "Approaches without progress were not bounded under frozen campaign time.");
            TestRunner.True(clock.Seconds > 0.6d && clock.ObservedFrames == frame - 1 && clock.SkippedSteps == 0, "The operational clock did not account its ticked frames exactly.");
        }

        private static void OperationalClockStallUnderFrozenCampaignTime()
        {
            // OFFLINE: a live move with movement opportunity (StallClockRuns true) and no displacement; campaign time frozen.
            var clock = new MountedDoorOperationalClock(); var life = new MountedDoorApproachLifecycle(); var frame = 1;
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.None, 10f, 0f)), "No first move.");
            var action = MountedDoorApproachAction.Wait; var frames = 0;
            while (action == MountedDoorApproachAction.Wait && frames < 10 * 60) { action = life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Live, 10f, 0f)); frames++; }
            TestRunner.Equal(MountedDoorApproachAction.Stalled, action, "A still mount under a live approach never stalled when campaign time was frozen.");
            TestRunner.True(clock.Seconds > MountedDoorApproachPolicy.StalledSecondsLimit && clock.Seconds < MountedDoorApproachPolicy.StalledSecondsLimit + 0.2d,
                "The stall did not fire at its operational bound (" + clock.Seconds.ToString("F2") + " s).");
            // The first approach end is the progress baseline (ApproachesWithoutProgress 0, BestDistance recorded); the stall counter resets.
            TestRunner.True(life.ApproachesWithoutProgress == 0 && life.BestDistance == 10f && life.StalledSeconds == 0f &&
                life.LastApproachEnd != null && life.LastApproachEnd.StartsWith("stalled for", StringComparison.Ordinal), "The stall was not recorded as one approach end.");
            // The next stall at the same distance counts as no progress, still on the operational clock.
            var retried = MountedDoorApproachAction.Wait; frames = 0;
            while (retried == MountedDoorApproachAction.Wait && frames < 10 * 60) { retried = life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.None, 10f, 0f)); frames++; }
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, retried, "The stalled approach was not retried after the cooldown.");
            action = MountedDoorApproachAction.Wait; frames = 0;
            while (action == MountedDoorApproachAction.Wait && frames < 10 * 60) { action = life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Live, 10f, 0f)); frames++; }
            TestRunner.True(action == MountedDoorApproachAction.Stalled && life.ApproachesWithoutProgress == 1, "A second stall at the same distance was not counted as no progress.");
        }

        private static void OperationalClockControlsChargeNothing()
        {
            var clock = new MountedDoorOperationalClock(); var life = new MountedDoorApproachLifecycle(); var frame = 1;
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.None, 10f, 0f)), "No first move.");
            // Paused frames: the engine's gameplay timestep is zero; nothing accrues.
            var before = clock.Seconds;
            for (var i = 0; i < 60; i++) TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Live, 10f, 0f, false, 0f)), "A paused frame acted.");
            TestRunner.True(clock.Seconds == before && life.StalledSeconds == 0f, "Paused frames were charged.");
            // Off-turn: the command is not ticked at all; the clock sees no frame. Modelled by skipping 20 s of frames.
            frame += 20 * 60;
            // Exhausted movement on the acting turn: ticked, the clock advances, but there is no movement opportunity.
            for (var i = 0; i < 20 * 60; i++) TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Live, 10f, 0f, false)), "A frame without movement opportunity stalled.");
            TestRunner.True(life.StalledSeconds == 0f && clock.Seconds > 19.9d, "Frames without movement opportunity accrued stall time, or the clock stopped.");
            // Resuming with opportunity: stillness is charged from the last re-sample (the previous frame) onward, one frame per
            // frame, never the 20 s of frames the command was not ticked nor the frames without opportunity.
            TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Live, 10f, 0f)), "The resume frame acted.");
            TestRunner.True(life.StalledSeconds <= FrameSeconds + 1e-6f, "The resume frame charged the waiting gap (" + life.StalledSeconds + ").");
            TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(Tick(clock, ref frame, MountedDoorApproachMoveState.Live, 10f, 0f)), "The second resumed frame acted.");
            TestRunner.True(life.StalledSeconds > FrameSeconds - 1e-6f && life.StalledSeconds <= 2f * FrameSeconds + 1e-6f, "Resumed stillness was not charged frame by frame (" + life.StalledSeconds + ").");
            // The clock itself: a repeated frame adds nothing; a non-finite, negative or oversized step is skipped, never charged.
            var seconds = clock.Seconds; var frames = clock.ObservedFrames;
            clock.Observe(frame - 1, FrameSeconds);
            TestRunner.True(clock.Seconds == seconds && clock.ObservedFrames == frames, "A repeated frame was charged.");
            clock.Observe(frame++, float.NaN); clock.Observe(frame++, -1f); clock.Observe(frame++, 5f);
            TestRunner.True(clock.Seconds == seconds && clock.SkippedSteps == 1 && clock.ObservedFrames == frames + 3, "An invalid or oversized step was charged.");
            TestRunner.True(MountedDoorApproachPolicy.StallClockRuns(true, false, true, false) == false && MountedDoorApproachPolicy.StallClockRuns(true, true, false, true) == false &&
                MountedDoorApproachPolicy.StallClockRuns(true, false, true, true) && MountedDoorApproachPolicy.StallClockRuns(true, false, false, false), "The stall gate changed.");
        }

        private static void OperationalClockSettlementUnresolvedUnderFrozenCampaignTime()
        {
            // OFFLINE: the door's graph update stays pending; campaign time frozen; the settlement wait is measured by the
            // operational clock and ends Unresolved at its bound: no sample, no search, no move.
            var clock = new MountedDoorOperationalClock(); var frame = 1;
            var life = new MountedDoorSettlementLifecycle(MountedDoorOccupancyPolicy.SettleOperationalSecondsLimit);
            var step = MountedDoorSettlementStep.Waiting; var frames = 0;
            while (step == MountedDoorSettlementStep.Waiting && frames < 20 * 60) { var now = clock.Observe(frame, FrameSeconds); step = life.Observe(false, now, frame); frame++; frames++; }
            TestRunner.Equal(MountedDoorSettlementStep.Unresolved, step, "The pending graph update never reached the settlement's operational bound under frozen campaign time.");
            TestRunner.True(clock.Seconds > MountedDoorOccupancyPolicy.SettleOperationalSecondsLimit && clock.Seconds < MountedDoorOccupancyPolicy.SettleOperationalSecondsLimit + 0.1d && frames < 200,
                "The settlement bound fired at the wrong operational time (" + clock.Seconds.ToString("F2") + " s over " + frames + " frames).");
            var sampled = 0; var searched = 0;
            var verdict = MountedDoorSettlementLifecycle.Resolve(step, () => { sampled++; return new MountedFootprintSample(true, 8, 8, 8); }, () => { searched++; return true; }, true, true, true);
            TestRunner.True(sampled == 0 && searched == 0 && !verdict.MoveAuthorized && !verdict.CollisionSafe && verdict.Disposition == MountedDoorRecoveryDisposition.Unresolved,
                "An unresolved settlement under frozen campaign time sampled, searched or authorized a move.");
        }

        private static void TurnBasedApproachEndDefersToLaterTurn()
        {
            // OFFLINE (review MAJOR-1): rider's acting turn, the mount's allocation runs out 4 m short; the native denial
            // branch finishes the delegated move. With the operational clock elapsing inside the turn, the lifecycle must
            // not retry (a zero-budget move would finish at once) nor count no-progress on that turn; it retries on the
            // next turn after the cooldown. Campaign time is frozen throughout.
            var clock = new MountedDoorOperationalClock(); var life = new MountedDoorApproachLifecycle(); var frame = 1;
            var turnOne = new object(); var turnTwo = new object();
            MountedDoorApproachObservation TurnObs(MountedDoorApproachMoveState move, float distance, object turn)
            { var o = Obs(clock.Observe(frame++, FrameSeconds), move, distance, 0f, false, false); o.TurnToken = turn; return o; }
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(TurnObs(MountedDoorApproachMoveState.None, 10f, turnOne)), "No first move on turn one.");
            TestRunner.Equal(MountedDoorApproachAction.Retry, life.Tick(TurnObs(MountedDoorApproachMoveState.Finished, 4f, turnOne)), "The finished approach was not recorded.");
            TestRunner.True(life.ApproachesWithoutProgress == 0 && life.BestDistance == 4f, "Progress on turn one was not credited.");
            // The rest of turn one: ten seconds of ticked frames, cooldown long elapsed, still no re-attempt.
            for (var i = 0; i < 10 * 60; i++) TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(TurnObs(MountedDoorApproachMoveState.None, 4f, turnOne)), "The approach was re-attempted on the turn it ended.");
            TestRunner.True(life.MoveStarts == 1 && life.ApproachesWithoutProgress == 0 && life.ExhaustedReason == null, "Turn one counted or exhausted a zero-budget retry.");
            // Turn two: the cooldown has elapsed, the re-attempt begins at once.
            TestRunner.Equal(MountedDoorApproachAction.BeginMove, life.Tick(TurnObs(MountedDoorApproachMoveState.None, 4f, turnTwo)), "A later turn did not re-attempt.");
            TestRunner.Equal(MountedDoorApproachAction.Retry, life.Tick(TurnObs(MountedDoorApproachMoveState.Finished, 4f, turnTwo)), "The second approach end was not recorded.");
            TestRunner.True(life.ApproachesWithoutProgress == 1, "No progress on turn two was not counted once.");
            for (var i = 0; i < 60; i++) TestRunner.Equal(MountedDoorApproachAction.Wait, life.Tick(TurnObs(MountedDoorApproachMoveState.None, 4f, turnTwo)), "Turn two re-attempted its own ended approach.");
            // Real time (no token): the cooldown alone governs, as before.
            var rt = new MountedDoorApproachLifecycle(); var rtClock = new MountedDoorOperationalClock(); var rtFrame = 1;
            rt.Tick(Obs(rtClock.Observe(rtFrame++, FrameSeconds), MountedDoorApproachMoveState.None, 10f, 0f));
            rt.Tick(Obs(rtClock.Observe(rtFrame++, FrameSeconds), MountedDoorApproachMoveState.Finished, 6f, 0f));
            var began = false; for (var i = 0; i < 60 && !began; i++) began = rt.Tick(Obs(rtClock.Observe(rtFrame++, FrameSeconds), MountedDoorApproachMoveState.None, 6f, 0f)) == MountedDoorApproachAction.BeginMove;
            TestRunner.True(began, "Real time no longer retries after the cooldown.");
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
