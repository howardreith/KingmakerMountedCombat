using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Sprint 2 DOOR-01 / SPACE-01: the offline decisions of one mounted door click.
    internal static class MountedDoorApproachPolicyTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("mounted door click replaces the mount's real-time player move", ClickReplacesRealTimePlayerMove);
            runner.Run("mounted door click on the same door retains the accepted approach", SameDoorClickRetainsAccepted);
            runner.Run("mounted door click on another door replaces the active approach", OtherDoorClickReplacesActive);
            runner.Run("mounted door click refuses other pair commands and busy mounts", RefusalsAreExact);
            runner.Run("mounted door re-approach is bounded by progress, not elapsed time", ReapproachIsProgressBounded);
            runner.Run("mounted door stall clock needs a running approach and a positive limit", StallDetectionIsExact);
            runner.Run("mounted door stall clock follows the mode and movement opportunity, not the paired-activation toggle", StallClockFollowsModeNotToggle);
            runner.Run("mounted door re-approach delay stays inside the native cooldown", ReapproachDelayIsNative);
            runner.Run("mounted door occupancy recovery is nearby-only and never transports", OccupancyDispositionIsExact);
            runner.Run("mounted door admission feedback names each disposition", FeedbackIsPerDisposition);
        }

        private static MountedDoorAdmissionDisposition Decide(
            bool otherPairCommandActive = false, bool activeDoor = false, bool sameDoor = false, bool turnBased = false,
            bool moveSlotOccupied = false, bool playerMove = false, bool otherSlotOccupied = false, bool queueEmpty = true)
        {
            return MountedDoorApproachPolicy.DecideAdmission(otherPairCommandActive, activeDoor, sameDoor, turnBased,
                moveSlotOccupied, playerMove, otherSlotOccupied, queueEmpty);
        }

        private static void ClickReplacesRealTimePlayerMove()
        {
            TestRunner.Equal(MountedDoorAdmissionDisposition.ReplaceMountMovement, Decide(moveSlotOccupied: true, playerMove: true),
                "A real-time player move on the mount did not yield to the door click.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.RejectMountBusy, Decide(moveSlotOccupied: true, playerMove: true, turnBased: true),
                "A turn-based mount move was replaced outside the rider-turn gates.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.RejectMountBusy, Decide(moveSlotOccupied: true, playerMove: false),
                "A non-player mount move was replaced.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.Admit, Decide(), "An idle mount was not admitted.");
        }

        private static void SameDoorClickRetainsAccepted()
        {
            TestRunner.Equal(MountedDoorAdmissionDisposition.RetainActiveSameDoor, Decide(activeDoor: true, sameDoor: true),
                "A repeated click on the approached door did not retain the accepted command.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.RetainActiveSameDoor,
                Decide(activeDoor: true, sameDoor: true, moveSlotOccupied: true, playerMove: true),
                "A repeated click while the delegated approach moves did not retain the accepted command.");
        }

        private static void OtherDoorClickReplacesActive()
        {
            TestRunner.Equal(MountedDoorAdmissionDisposition.ReplaceActiveDoor, Decide(activeDoor: true, sameDoor: false),
                "A click on another door did not replace the active approach.");
        }

        private static void RefusalsAreExact()
        {
            TestRunner.Equal(MountedDoorAdmissionDisposition.RejectOtherPairCommand, Decide(otherPairCommandActive: true),
                "An active pair attack or ground admission did not refuse the door click.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.RejectOtherPairCommand, Decide(otherPairCommandActive: true, activeDoor: true, sameDoor: true),
                "Pair command ownership was not decided before door retention.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.RejectMountBusy, Decide(otherSlotOccupied: true),
                "A mount Standard/Free/Swift command was replaced by the door click.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.RejectMountBusy, Decide(queueEmpty: false),
                "A queued mount command was replaced by the door click.");
            TestRunner.Equal(MountedDoorAdmissionDisposition.RejectMountBusy, Decide(moveSlotOccupied: true, playerMove: true, queueEmpty: false),
                "A player move with a queue behind it was replaced.");
        }

        private static void ReapproachIsProgressBounded()
        {
            TestRunner.True(MountedDoorApproachPolicy.ApproachProgressed(float.PositiveInfinity, 12f, 0.25f), "The first approach end was not progress.");
            TestRunner.True(MountedDoorApproachPolicy.ApproachProgressed(12f, 11.7f, 0.25f), "A 0.3 m gain was not progress.");
            TestRunner.True(!MountedDoorApproachPolicy.ApproachProgressed(12f, 11.9f, 0.25f), "A 0.1 m gain counted as progress.");
            TestRunner.True(!MountedDoorApproachPolicy.ApproachProgressed(12f, 12.5f, 0.25f), "Moving away counted as progress.");
            TestRunner.True(!MountedDoorApproachPolicy.ApproachProgressed(12f, float.NaN, 0.25f), "A non-finite distance counted as progress.");
            TestRunner.True(!MountedDoorApproachPolicy.ApproachExhausted(0, 3) && !MountedDoorApproachPolicy.ApproachExhausted(2, 3),
                "Fewer than the bounded approaches were exhausted.");
            TestRunner.True(MountedDoorApproachPolicy.ApproachExhausted(3, 3) && MountedDoorApproachPolicy.ApproachExhausted(4, 3),
                "The bounded approaches were not exhausted.");
            TestRunner.True(!MountedDoorApproachPolicy.ApproachExhausted(5, 0), "A zero bound exhausted approaches.");
            TestRunner.Equal(3, MountedDoorApproachPolicy.MaximumApproachesWithoutProgress, "The approach bound changed.");
        }

        private static void StallDetectionIsExact()
        {
            TestRunner.True(MountedDoorApproachPolicy.MovementStalled(4.5f, 4f, true), "A 4.5 s still mount was not stalled.");
            TestRunner.True(!MountedDoorApproachPolicy.MovementStalled(4.5f, 4f, false), "A finished approach was stalled.");
            TestRunner.True(!MountedDoorApproachPolicy.MovementStalled(3.9f, 4f, true), "An approach inside the limit was stalled.");
            TestRunner.True(!MountedDoorApproachPolicy.MovementStalled(4.5f, 0f, true), "A zero limit stalled the approach.");
            TestRunner.True(!MountedDoorApproachPolicy.MovementStalled(float.NaN, 4f, true), "A non-finite clock stalled the approach.");
        }

        private static void StallClockFollowsModeNotToggle()
        {
            // The production-facing combination: a real-time approach with paired activation enabled is the owner's
            // configuration; the clock must run there (the former !NativePartnerMovement guard disabled it). The
            // toggle is not an input of the decision at all; only mode and movement opportunity are.
            TestRunner.True(MountedDoorApproachPolicy.StallClockRuns(true, false, false, true), "A running real-time approach did not count stillness.");
            TestRunner.True(MountedDoorApproachPolicy.StallClockRuns(true, false, false, false), "Real time consulted the turn-based movement opportunity.");
            TestRunner.True(!MountedDoorApproachPolicy.StallClockRuns(false, false, false, true), "An approach that is not running counted stillness.");
            TestRunner.True(!MountedDoorApproachPolicy.StallClockRuns(true, true, false, true), "A paused real-time game counted stillness.");
            TestRunner.True(MountedDoorApproachPolicy.StallClockRuns(true, false, true, true), "A rider-turn approach with movement budget did not count stillness.");
            TestRunner.True(!MountedDoorApproachPolicy.StallClockRuns(true, false, true, false),
                "An off-turn or budget-exhausted turn-based mount was read as pathfinding failure.");
            TestRunner.True(!MountedDoorApproachPolicy.StallClockRuns(true, true, true, true), "A paused turn-based game counted stillness.");
            // Sustained progress never stalls regardless of the clock: the limit applies to accumulated stillness only.
            var stillSeconds = 0f;
            for (var tick = 0; tick < 100; tick++)
            {
                var displaced = tick % 3 != 2; // moves two ticks of three
                if (MountedDoorApproachPolicy.StallClockRuns(true, false, false, true)) stillSeconds = displaced ? 0f : stillSeconds + 0.1f;
                TestRunner.True(!MountedDoorApproachPolicy.MovementStalled(stillSeconds, MountedDoorApproachPolicy.StalledSecondsLimit, true),
                    "Sustained progress was reported as a stall at tick " + tick + ".");
            }
            // Genuine no-progress in real time with paired activation on: the clock accumulates and the limit trips.
            stillSeconds = 0f;
            for (var tick = 0; tick < 45; tick++) if (MountedDoorApproachPolicy.StallClockRuns(true, false, false, false)) stillSeconds += 0.1f;
            TestRunner.True(MountedDoorApproachPolicy.MovementStalled(stillSeconds, MountedDoorApproachPolicy.StalledSecondsLimit, true),
                "A real-time approach that made no progress for 4.5 s was not stalled.");
        }

        private static void ReapproachDelayIsNative()
        {
            TestRunner.Equal(0.2f, MountedDoorApproachPolicy.ReapproachDelaySeconds(0f), "The minimum delay is not the native 0.2 s.");
            TestRunner.Equal(0.6f, MountedDoorApproachPolicy.ReapproachDelaySeconds(1f), "The maximum delay is not the native 0.6 s.");
            TestRunner.Equal(0.2f, MountedDoorApproachPolicy.ReapproachDelaySeconds(-5f), "A sample below zero escaped the cooldown.");
            TestRunner.Equal(0.6f, MountedDoorApproachPolicy.ReapproachDelaySeconds(7f), "A sample above one escaped the cooldown.");
            TestRunner.Equal(0.2f, MountedDoorApproachPolicy.ReapproachDelaySeconds(float.NaN), "A non-finite sample escaped the cooldown.");
            var mid = MountedDoorApproachPolicy.ReapproachDelaySeconds(0.5f);
            TestRunner.True(Math.Abs(mid - 0.4f) < 1e-6f, "The midpoint delay is not 0.4 s.");
        }

        private static void OccupancyDispositionIsExact()
        {
            TestRunner.Equal(MountedDoorOccupancyDisposition.Valid, MountedDoorApproachPolicy.DecideOccupancy(true, false, float.PositiveInfinity, 1.5f),
                "A mount on the navmesh was recovered.");
            TestRunner.Equal(MountedDoorOccupancyDisposition.RepathToNearest, MountedDoorApproachPolicy.DecideOccupancy(false, true, 0.8f, 1.5f),
                "A nearby walkable node was not offered as the repath target.");
            TestRunner.Equal(MountedDoorOccupancyDisposition.RepathToNearest, MountedDoorApproachPolicy.DecideOccupancy(false, true, 1.5f, 1.5f),
                "The recovery radius boundary was excluded.");
            TestRunner.Equal(MountedDoorOccupancyDisposition.Unrecoverable, MountedDoorApproachPolicy.DecideOccupancy(false, true, 1.6f, 1.5f),
                "A node beyond the recovery radius was transported to.");
            TestRunner.Equal(MountedDoorOccupancyDisposition.Unrecoverable, MountedDoorApproachPolicy.DecideOccupancy(false, false, 0.1f, 1.5f),
                "An unwalkable nearest node was used for recovery.");
            TestRunner.Equal(MountedDoorOccupancyDisposition.Unrecoverable, MountedDoorApproachPolicy.DecideOccupancy(false, true, float.NaN, 1.5f),
                "A non-finite node distance was used for recovery.");
        }

        private static void FeedbackIsPerDisposition()
        {
            foreach (MountedDoorAdmissionDisposition disposition in Enum.GetValues(typeof(MountedDoorAdmissionDisposition)))
            {
                var feedback = MountedDoorApproachPolicy.Feedback(disposition, "Horse");
                TestRunner.True(!string.IsNullOrWhiteSpace(feedback), "No feedback for " + disposition + ".");
            }
            TestRunner.True(MountedDoorApproachPolicy.Feedback(MountedDoorAdmissionDisposition.ReplaceMountMovement, "Horse").Contains("Horse"),
                "Replacement feedback does not name the mount.");
            TestRunner.True(MountedDoorApproachPolicy.Feedback(MountedDoorAdmissionDisposition.RetainActiveSameDoor, "Horse").Contains("continues"),
                "Retention feedback does not say the accepted approach continues.");
        }
    }
}
