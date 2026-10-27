using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // F4 r30: the first pair's return goal (its captured start) lay 0.86 m from pair B's Horse, inside the native
    // real-time avoidance disk (0.5 + 0.5 m out of combat), so the native return could not succeed there.
    internal static class DoorReturnGoalPolicyTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("door return goal refuses F4 r30's start beside pair B's Horse", RefusesF4Start);
            runner.Run("door return goal keeps the F1 and F3 starts clear of the other pair", KeepsClearStarts);
            runner.Run("door return goal keeps the qualifying-door distance and navmesh terms", DoorAndNavmeshTerms);
            runner.Run("door return goal tries the captured start first and every ring bearing once", CandidateOrder);
        }

        private static double Admit(double distanceToOther) =>
            DoorReturnGoalPolicy.Slack(distanceToOther, 0.5, 0.5);

        private static void RefusesF4Start()
        {
            var slack = Admit(0.86);
            TestRunner.True(slack < 0, "F4's 0.86 m start was clear of the Horse's footprint.");
            TestRunner.Equal("inside-unit-footprint", DoorReturnGoalPolicy.Reason(true, 0, 0, 8.6, 2, 28, slack), "F4 start was misnamed.");
            // Exactly the native obstacle disk is still refused: the margin covers the 0.3 m approach radius.
            TestRunner.True(!DoorReturnGoalPolicy.Admits(true, 0, 0, 8.6, 2, 28, Admit(1.0)), "A goal on the obstacle disk edge was admitted.");
            TestRunner.True(!DoorReturnGoalPolicy.Admits(true, 0, 0, 8.6, 2, 28, Admit(1.49)), "A goal inside the margin was admitted.");
            TestRunner.True(!DoorReturnGoalPolicy.Admits(true, 0, 0, 8.6, 2, 28, double.NaN), "A nonnumeric slack was admitted.");
        }

        private static void KeepsClearStarts()
        {
            TestRunner.True(DoorReturnGoalPolicy.Admits(true, 0, 0, 8.6, 2, 28, Admit(3.10)), "F3's 3.10 m start was refused.");
            TestRunner.True(DoorReturnGoalPolicy.Admits(true, 0, 0, 8.6, 2, 28, Admit(2.52)), "F1's 2.52 m start was refused.");
            TestRunner.True(DoorReturnGoalPolicy.Admits(true, 0, 0, 8.6, 2, 28, Admit(1.5)), "The exact clearance bound was refused.");
            // Combat corpulence is honoured, not the out-of-combat constant.
            TestRunner.True(DoorReturnGoalPolicy.Slack(2.0, 1.0, 1.0) < 0, "A combat-sized footprint was ignored.");
        }

        private static void DoorAndNavmeshTerms()
        {
            var clear = Admit(3);
            TestRunner.Equal("inside-door-approach", DoorReturnGoalPolicy.Reason(true, 0, 0, 2.5, 2, 28, clear), "Door band was admitted.");
            TestRunner.Equal("admitted", DoorReturnGoalPolicy.Reason(true, 0, 0, 2.51, 2, 28, clear), "Just outside the door band was refused.");
            TestRunner.Equal("outside-door-scan-radius", DoorReturnGoalPolicy.Reason(true, 0, 0, 28.1, 2, 28, clear), "Beyond the scan radius was admitted.");
            TestRunner.Equal("no-walkable-native-node", DoorReturnGoalPolicy.Reason(false, 0, 0, 8, 2, 28, clear), "Unwalkable was misnamed.");
            TestRunner.Equal("off-navmesh", DoorReturnGoalPolicy.Reason(true, 0.11, 0, 8, 2, 28, clear), "A far clamp was admitted.");
            TestRunner.Equal("navmesh-trace-blocked", DoorReturnGoalPolicy.Reason(true, 0, 0.06, 8, 2, 28, clear), "A blocked trace was admitted.");
        }

        private static void CandidateOrder()
        {
            TestRunner.Equal(1 + 9 * 16, DoorReturnGoalPolicy.CandidateCount, "Candidate count changed.");
            DoorReturnGoalPolicy.Offset(0, out var ring, out var bearing, out var dx, out var dz);
            TestRunner.True(ring == 0 && bearing == 0 && dx == 0 && dz == 0, "The captured start is not the first candidate.");
            DoorReturnGoalPolicy.Offset(1, out ring, out bearing, out dx, out dz);
            TestRunner.True(ring == 0.5f && bearing == 0 && Math.Abs(dz - 0.5f) < 1e-6 && Math.Abs(dx) < 1e-6, "First ring candidate is not +z 0.5 m.");
            DoorReturnGoalPolicy.Offset(5, out ring, out bearing, out dx, out dz);
            TestRunner.True(bearing == 90f && Math.Abs(dx - 0.5f) < 1e-6 && Math.Abs(dz) < 1e-6, "Bearing order is not clockwise from +z.");
            DoorReturnGoalPolicy.Offset(DoorReturnGoalPolicy.CandidateCount - 1, out ring, out bearing, out dx, out dz);
            TestRunner.True(ring == 5f && bearing == 337.5f, "Last candidate is not the outer ring's last bearing.");
            var threw = false;
            try { DoorReturnGoalPolicy.Offset(DoorReturnGoalPolicy.CandidateCount, out ring, out bearing, out dx, out dz); }
            catch (ArgumentOutOfRangeException) { threw = true; }
            TestRunner.True(threw, "An out-of-range candidate was accepted.");
        }
    }
}
