using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeGroundFixturePolicy
    {
        // Intersections of a short rider step with a narrow annulus around the mount.
        // Neither world yaw nor the mount's facing can exclude a valid tangent step.
        internal static IEnumerable<PoseVector3> ActingProposals(PoseVector3 rider, PoseVector3 mount)
        {
            if (!rider.IsFinite || !mount.IsFinite) yield break;
            var dx = (double)rider.X - mount.X; var dz = (double)rider.Z - mount.Z;
            var separation = Math.Sqrt(dx * dx + dz * dz);
            if (separation < 0.25) yield break;
            dx /= separation; dz /= separation;
            foreach (var step in new[] { 0.6, 0.5, 0.7 })
                foreach (var increase in new[] { 0.075, 0.025, 0.125 })
                {
                    var next = separation + increase;
                    var radial = (next * next - separation * separation - step * step) / (2 * separation);
                    var square = step * step - radial * radial;
                    if (square < 0) continue;
                    var tangent = Math.Sqrt(square);
                    foreach (var side in new[] { -1, 1 })
                        yield return new PoseVector3((float)(rider.X + dx * radial - dz * tangent * side), rider.Y,
                            (float)(rider.Z + dz * radial + dx * tangent * side));
                }
        }

        internal static bool IsActingStep(double beforeSeparation, double separation, double travel,
            double routeResidual, double footprintResidual, bool occupied)
        {
            foreach (var value in new[] { beforeSeparation, separation, travel, routeResidual, footprintResidual })
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) return false;
            return !occupied && beforeSeparation >= 0.25 && Math.Abs(travel - 0.6) <= 0.15 &&
                separation >= beforeSeparation && separation <= beforeSeparation + 0.15 &&
                routeResidual <= MountedCombatSpatialPolicy.DiagnosticPlacementTolerance && footprintResidual < 0.001;
        }

        internal static bool IsPreCombatPosition(double requestedSeparation, double separation, double travel,
            double routeResidual, double footprintResidual, bool occupied)
        {
            return requestedSeparation >= 2 && requestedSeparation <= 2.65 && travel <= 4 &&
                IsClear(requestedSeparation, separation, travel, routeResidual, footprintResidual, occupied);
        }

        internal static bool IsClear(double requestedSeparation, double separation, double travel,
            double routeResidual, double footprintResidual, bool occupied)
        {
            foreach (var value in new[] { requestedSeparation, separation, travel, routeResidual, footprintResidual })
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) return false;
            return !occupied && requestedSeparation > 0 && Math.Abs(separation - requestedSeparation) <= 0.45 &&
                travel >= 0.25 && routeResidual < 0.001 && footprintResidual < 0.001;
        }
    }
}
