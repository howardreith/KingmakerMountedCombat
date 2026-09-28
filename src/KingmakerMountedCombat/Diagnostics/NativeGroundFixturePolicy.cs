using System;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class NativeGroundFixturePolicy
    {
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
