using System;

namespace KingmakerMountedCombat.Diagnostics
{
    // F3 r07/r08: the independent Horse's 0.5 m ordinary ground click toward the target finished Success after
    // 0.011 s with no movement and no move spent. Kingmaker 2.1.7b contracts (local metadata):
    // - in turn-based combat ClickGroundHandler.OnClick replaces the clicked point with the last point of
    //   PathVisualizer.CurrentPathForUnit, and RunCommand issues UnitMoveTo(point, 0.3f);
    // - PathVisualizer.UpdatePath replaces the held path only when the new request completes, so an unreachable
    //   destination leaves the previous path in place (ABPathWithClearance plans on the TB grid, where other units and
    //   the erosion tags of the mover's corpulence block nodes outside its start clearance);
    // - UnitCommand.ShouldUnitApproach is false when SqrMechanicsDistance(ApproachPoint, Executor.Position) is within
    //   ApproachRadius^2: the command starts, acts and succeeds where the unit stands.
    // The F3 record shows exactly that first eligibility frame: shouldApproach=false before any movement (F2's PASS
    // shows true). A step destination is therefore admitted only when the native preview the click will consume is the
    // fresh, completed path for that destination, ends within the click's native approach radius of it and lies far
    // enough from the Horse that the admitted UnitMoveTo must approach. Nothing is granted, refunded or prepared.
    internal static class NativeGroundStepDestinationPolicy
    {
        // ClickGroundHandler.RunCommand: new UnitMoveTo(p, 0.3f).
        internal const float NativeGroundApproachRadius = 0.3f;
        // The admitted command must leave at least this much native approach beyond its own radius.
        internal const float MinimumNativeApproach = 0.1f;
        internal const float StepDistance = 0.5f;

        // Toward the target first (the F2 PASS geometry keeps its exact destination), then alternately either side.
        internal static float[] BearingOffsets()
        {
            var offsets = new float[16]; offsets[0] = 0f;
            for (var i = 1; i < 16; i++) offsets[i] = (i % 2 == 1 ? 1f : -1f) * 22.5f * ((i + 1) / 2);
            offsets[15] = 180f;
            return offsets;
        }

        internal static bool Admits(bool fresh, bool complete, double endResidual, double displacement) =>
            fresh && complete && !double.IsNaN(endResidual) && !double.IsInfinity(endResidual) &&
            !double.IsNaN(displacement) && !double.IsInfinity(displacement) &&
            endResidual <= NativeGroundApproachRadius && displacement >= NativeGroundApproachRadius + MinimumNativeApproach;

        internal static string Reason(bool fresh, bool complete, double endResidual, double displacement)
        {
            if (!complete) return "no-completed-native-preview";
            if (!fresh) return "stale-native-preview";
            if (double.IsNaN(endResidual) || double.IsInfinity(endResidual) || endResidual > NativeGroundApproachRadius) return "preview-ends-away-from-destination";
            if (double.IsNaN(displacement) || double.IsInfinity(displacement) || displacement < NativeGroundApproachRadius + MinimumNativeApproach) return "preview-ends-inside-approach-radius";
            return "admitted";
        }
    }
}
