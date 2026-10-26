using System;
using KingmakerMountedCombat.Diagnostics;

namespace KingmakerMountedCombat.Tests
{
    // F5 hardening: the world scenario's fixed 2 m ground moves (F4 r30's horse goal lay 0.83 m from rider A) and the row 57
    // prone target's turn-based approach (its destination fan ignored pair B's Horse).
    internal static class GroundGoalFootprintPolicyTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("world route goal refuses F4 r30's horse goal beside rider A and keeps F3's", RouteF4Geometry);
            runner.Run("world route goal keeps the navmesh terms of the door policy", RouteNavmeshTerms);
            runner.Run("world route goal tries the declared direction first and rotates as Unity", RouteOrder);
            runner.Run("prone grid footprint keeps the F2/F3 Horse geometry and refuses box corners", GridGeometry);
        }

        private static void RouteF4Geometry()
        {
            var f4 = GroundGoalFootprintPolicy.RouteSlack(0.826, 0.5, 0.5);
            TestRunner.Equal("inside-unit-footprint", GroundGoalFootprintPolicy.RouteReason(true, 0, 0, f4), "F4's horse goal was admitted.");
            var f3 = GroundGoalFootprintPolicy.RouteSlack(2.326, 0.5, 0.5);
            TestRunner.Equal("admitted", GroundGoalFootprintPolicy.RouteReason(true, 0, 0, f3), "F3's horse goal was refused.");
            TestRunner.Equal("admitted", GroundGoalFootprintPolicy.RouteReason(true, 0, 0, GroundGoalFootprintPolicy.RouteSlack(1.5, 0.5, 0.5)), "The exact bound was refused.");
            TestRunner.Equal("inside-unit-footprint", GroundGoalFootprintPolicy.RouteReason(true, 0, 0, GroundGoalFootprintPolicy.RouteSlack(1.49, 0.5, 0.5)), "The margin was not kept.");
            TestRunner.Equal("inside-unit-footprint", GroundGoalFootprintPolicy.RouteReason(true, 0, 0, double.NaN), "A nonnumeric slack was admitted.");
        }

        private static void RouteNavmeshTerms()
        {
            TestRunner.Equal("no-walkable-native-node", GroundGoalFootprintPolicy.RouteReason(false, 0, 0, 1), "Unwalkable was misnamed.");
            TestRunner.Equal("off-navmesh", GroundGoalFootprintPolicy.RouteReason(true, 0.11, 0, 1), "A far clamp was admitted.");
            TestRunner.Equal("admitted", GroundGoalFootprintPolicy.RouteReason(true, 0.1, 0.05, 0), "The navmesh bounds changed.");
            TestRunner.Equal("navmesh-trace-blocked", GroundGoalFootprintPolicy.RouteReason(true, 0, 0.06, 1), "A blocked trace was admitted.");
            TestRunner.True(GroundGoalFootprintPolicy.RouteSlack(2.0, 1.0, 1.0) < 0, "A combat-sized footprint was ignored.");
        }

        private static void RouteOrder()
        {
            var offsets = GroundGoalFootprintPolicy.RouteBearingOffsets();
            TestRunner.Equal(16, offsets.Length, "Route candidate count changed.");
            TestRunner.True(offsets[0] == 0f && offsets[1] == 22.5f && offsets[2] == -22.5f && offsets[15] == 180f, "Route bearing order changed.");
            TestRunner.Equal(2f, GroundGoalFootprintPolicy.RouteDistance, "The fixed two-metre route length changed.");
            // Quaternion.Euler(0, 90, 0) * Vector3.right == -z; * Vector3.forward == +x.
            GroundGoalFootprintPolicy.RouteDirection(1, 0, 90, out var x, out var z);
            TestRunner.True(Math.Abs(x) < 1e-9 && Math.Abs(z + 1) < 1e-9, "Right rotated by 90 degrees is not -z.");
            GroundGoalFootprintPolicy.RouteDirection(0, 1, 90, out x, out z);
            TestRunner.True(Math.Abs(x - 1) < 1e-9 && Math.Abs(z) < 1e-9, "Forward rotated by 90 degrees is not +x.");
            GroundGoalFootprintPolicy.RouteDirection(-1, 0, 0, out x, out z);
            TestRunner.True(x == -1 && z == 0, "The declared direction is not kept at offset 0.");
        }

        private static void GridGeometry()
        {
            // F3 r57: the destination lay (1.11, 2.20) m from pair B's Horse (target 0.7, Horse 0.5) and (0.69, 2.78) m
            // from the Mammoth (0.9); F2: (1.02, 2.71) and (0.65, 2.49). All clear: the first proposal is unchanged.
            TestRunner.True(GroundGoalFootprintPolicy.GridAdmits(GroundGoalFootprintPolicy.GridSlack(1.11, 2.20, 0.7, 0.5)), "F3's Horse geometry was refused.");
            TestRunner.True(GroundGoalFootprintPolicy.GridAdmits(GroundGoalFootprintPolicy.GridSlack(0.69, 2.78, 0.7, 0.9)), "F3's Mammoth geometry was refused.");
            TestRunner.True(GroundGoalFootprintPolicy.GridAdmits(GroundGoalFootprintPolicy.GridSlack(-1.02, -2.71, 0.7, 0.5)), "F2's Horse geometry was refused.");
            TestRunner.True(GroundGoalFootprintPolicy.GridAdmits(GroundGoalFootprintPolicy.GridSlack(0.65, 2.49, 0.7, 0.9)), "F2's Mammoth geometry was refused.");
            // A diagonal corner of the native box: Euclidean 1.84 m keeps the disk bound, Chebyshev 1.3 m does not.
            TestRunner.True(!GroundGoalFootprintPolicy.GridAdmits(GroundGoalFootprintPolicy.GridSlack(1.3, 1.3, 0.7, 0.5)), "A box corner was admitted.");
            TestRunner.True(GroundGoalFootprintPolicy.GridAdmits(GroundGoalFootprintPolicy.GridSlack(1.7, 0.0, 0.7, 0.5)), "The exact bound was refused.");
            TestRunner.True(!GroundGoalFootprintPolicy.GridAdmits(double.NaN), "A nonnumeric slack was admitted.");
        }
    }
}
