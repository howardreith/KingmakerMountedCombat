using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // F5 hardening of the two remaining diagnostic goals that ignored other units' native footprints, by the accepted
    // DoorReturnGoalPolicy / NativeGroundStepDestinationPolicy pattern: a pure, read-only choice of a native-reachable goal
    // clear of every other unit's footprint (corpulence sum + DoorReturnGoalPolicy.ClearanceMargin), recorded before any
    // assertion, the declared goal kept exactly when it is clear, and no admissible goal refused as uncovered.
    //
    // 1. The two-pair world scenario's fixed 2 m ground moves (Chunk7WorldScenarioEngine, mammoth, horse and
    //    unmounted-control; rows 30, 40 and 41). F4 r30's horse goal lay 0.83 m from rider A and 0.91 m from the Mammoth.
    //    These are real-time, out-of-combat clicks (ClickGroundHandler.MoveSelectedUnitsToPoint: GetNearestNode clamp,
    //    UnitMoveTo(p, 0.3f)), so the obstacle is DoorReturnGoalPolicy's real-time avoidance disk
    //    (UnitMovementAgent.UpdateAvoidance: Corpulence(mover) + Corpulence(other), 0.5 each out of combat; a 7 s stall or a
    //    completion outside the approach radius interrupts the Move). Candidates keep the declared 2 m route length: the
    //    declared direction first (the old fixed goal), then NativeGroundStepDestinationPolicy.BearingOffsets() around it.
    //
    // 2. The row 57 prone target's own-turn approach (Chunk7CombatScenario, UnitMoveTo(destination, 0.1f) in turn-based
    //    combat). Kingmaker 2.1.7b contracts (local metadata): UnitMovementAgent.PathTo in turn-based combat plans on the
    //    grid graph (graph mask 2) through SeekerWithClearance.StartPathWithClerance (ABPathWithClearance, approach radius
    //    ending condition, traversable tags excluding the erosion tags of the mover's corpulence); TickMovement skips
    //    UpdateAvoidance in turn-based combat, so the real-time disk does not apply. CombatController.StartTurn marks the grid
    //    nodes inside an axis-aligned box of size 2 x Corpulence around every other turn-based unit unwalkable (none when
    //    the acting unit's avoidance is disabled) and ErodeWalkableArea grows them by the mover's corpulence in cells. A
    //    goal inside that eroded box has no grid path: OnPathComplete reports OnPathNotFound and the command is not
    //    Success. The PathVisualizer preview is consumed only by a ground click (ClickGroundHandler.OnClick), not by a
    //    directly run UnitMoveTo. The footprint is therefore measured as a box: the horizontal Chebyshev distance must
    //    keep corpulence sum + margin, which also covers the box's diagonal corners (a Euclidean bound would not).
    // Nothing is moved, paused, granted, refunded or prepared to make a goal admissible.
    internal static class GroundGoalFootprintPolicy
    {
        internal const string RouteContract = "world-route-goal-clear-of-units/v1";
        internal const string GridContract = "tb-grid-unit-footprint/v1";
        // The scenario's fixed two-metre route length (its reader's endpoint bounds are unchanged).
        internal const float RouteDistance = 2f;
        internal const string GridRefusal = "inside a combat unit's native grid footprint";

        // The declared direction first, then alternately either side in 22.5 degree steps, 180 last.
        internal static float[] RouteBearingOffsets() => NativeGroundStepDestinationPolicy.BearingOffsets();

        // UnityEngine.Quaternion.Euler(0, offset, 0) applied to the horizontal declared direction (clockwise seen from +y).
        internal static void RouteDirection(double directionX, double directionZ, double bearingOffset, out double x, out double z)
        {
            var radians = bearingOffset * Math.PI / 180.0; var cos = Math.Cos(radians); var sin = Math.Sin(radians);
            x = directionX * cos + directionZ * sin; z = -directionX * sin + directionZ * cos;
        }

        // Real-time clearance against one unit (out of combat): DoorReturnGoalPolicy's disk slack.
        internal static double RouteSlack(double distance, double moverCorpulence, double otherCorpulence) =>
            DoorReturnGoalPolicy.Slack(distance, moverCorpulence, otherCorpulence);

        internal static string RouteReason(bool walkable, double clampResidual, double traceResidual, double minimumSlack)
        {
            if (!walkable) return "no-walkable-native-node";
            if (!Finite(clampResidual) || clampResidual > DoorReturnGoalPolicy.MaximumClampResidual) return "off-navmesh";
            if (!Finite(traceResidual) || traceResidual > DoorReturnGoalPolicy.MaximumTraceResidual) return "navmesh-trace-blocked";
            if (!Finite(minimumSlack) || minimumSlack < 0) return "inside-unit-footprint";
            return "admitted";
        }

        // Turn-based grid clearance against one unit: horizontal Chebyshev distance minus (both corpulences + margin).
        internal static double GridSlack(double dx, double dz, double moverCorpulence, double otherCorpulence) =>
            Math.Max(Math.Abs(dx), Math.Abs(dz)) - (moverCorpulence + otherCorpulence + DoorReturnGoalPolicy.ClearanceMargin);

        internal static bool GridAdmits(double minimumSlack) => Finite(minimumSlack) && minimumSlack >= 0;

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
