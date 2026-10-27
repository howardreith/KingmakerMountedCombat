using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // F4 r30: the door probe's first pair returned to the mount's own captured position (firstStart), where pair B's
    // Horse stood 0.86 m away. Kingmaker 2.1.7b contracts (local metadata):
    // - ClickGroundHandler.MoveSelectedUnitsToPoint with one selected unit clamps the point to
    //   ObstacleAnalyzer.GetNearestNode(point).clampedPosition and RunCommand issues UnitMoveTo(point, 0.3f);
    // - in real time UnitMovementAgent.UpdateAvoidance adds remembered units as obstacles of radius
    //   Corpulence(mover) + Corpulence(other) (UnitMovementAgentBase.Corpulence is 0.5 out of combat, where every obstacle
    //   is soft), ignoring a stationary one beyond the destination only while the mover is still outside that disk; a goal
    //   inside the disk is therefore approached under avoidance steering, and when the agent stalls (InterruptMovement
    //   after 7 s stuck) or completes outside its approach radius (CompleteMovement -> InterruptMovement),
    //   UnitEntityView.OnMovementInterrupted interrupts the Move command: not Success. Success implies the mover reached
    //   the 0.3 m approach radius (UnitCommand.ShouldUnitApproach re-issues the move until it does).
    // A return goal is therefore admitted only when it is on the walkable navmesh, reachable along the navmesh from the
    // captured start, still satisfies the qualifying-door distance predicate (outside the interaction radius plus 0.5 m,
    // within the scan radius), and lies outside every other unit's native avoidance disk by at least the native approach
    // radius plus a margin. The captured start is tried first, so a clear start keeps the F1/F3 geometry.
    // Nothing is moved, paused, granted, refunded or prepared to make a goal admissible.
    internal static class DoorReturnGoalPolicy
    {
        internal const string Contract = "door-return-goal-clear-of-units/v1";
        // ClickGroundHandler.RunCommand: new UnitMoveTo(p, 0.3f).
        internal const float NativeGroundApproachRadius = 0.3f;
        // Required space beyond the corpulence-sum obstacle disk: the approach radius plus 0.2 m.
        internal const float ClearanceMargin = 0.5f;
        // A requested point whose nearest walkable node lies farther away is not the goal the ring describes.
        internal const float MaximumClampResidual = 0.1f;
        // The straight navmesh trace from the captured start must reach the clamped goal.
        internal const float MaximumTraceResidual = 0.05f;
        // Units this close to the captured start are recorded and considered (ring 5 m + corpulence sums).
        internal const float UnitConsiderationRadius = 11f;
        internal const int MaximumUnits = 256;
        internal static readonly float[] RingRadii = { 0.5f, 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f, 5f };
        internal const int BearingsPerRing = 16;

        // Index 0 is the captured start itself; then each ring outward, bearings clockwise from +z in 22.5 degree steps.
        internal static int CandidateCount => 1 + RingRadii.Length * BearingsPerRing;

        internal static void Offset(int index, out float ring, out float bearing, out float dx, out float dz)
        {
            if (index < 0 || index >= CandidateCount) throw new ArgumentOutOfRangeException(nameof(index));
            if (index == 0) { ring = 0f; bearing = 0f; dx = 0f; dz = 0f; return; }
            ring = RingRadii[(index - 1) / BearingsPerRing];
            bearing = 360f / BearingsPerRing * ((index - 1) % BearingsPerRing);
            var radians = bearing * Math.PI / 180.0;
            dx = (float)(Math.Sin(radians) * ring); dz = (float)(Math.Cos(radians) * ring);
        }

        // Clearance slack against one unit: horizontal distance minus (both native corpulences + margin).
        internal static double Slack(double distance, double moverCorpulence, double otherCorpulence) =>
            distance - (moverCorpulence + otherCorpulence + ClearanceMargin);

        internal static string Reason(bool walkable, double clampResidual, double traceResidual, double doorDistance,
            double doorRadius, double scanRadius, double minimumSlack)
        {
            if (!walkable) return "no-walkable-native-node";
            if (!Finite(clampResidual) || clampResidual > MaximumClampResidual) return "off-navmesh";
            if (!Finite(traceResidual) || traceResidual > MaximumTraceResidual) return "navmesh-trace-blocked";
            if (!Finite(doorDistance) || doorDistance <= doorRadius + 0.5) return "inside-door-approach";
            if (doorDistance > scanRadius) return "outside-door-scan-radius";
            if (!Finite(minimumSlack) || minimumSlack < 0) return "inside-unit-footprint";
            return "admitted";
        }

        internal static bool Admits(bool walkable, double clampResidual, double traceResidual, double doorDistance,
            double doorRadius, double scanRadius, double minimumSlack) =>
            Reason(walkable, clampResidual, traceResidual, doorDistance, doorRadius, scanRadius, minimumSlack) == "admitted";

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
