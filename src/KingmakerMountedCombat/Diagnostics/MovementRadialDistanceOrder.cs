using System;

namespace KingmakerMountedCombat.Diagnostics
{
    internal static class MovementRadialDistanceOrder
    {
        public static float[] CreateLocalFirst(float minimum, float middle, float maximum)
        {
            if (float.IsNaN(minimum) || float.IsInfinity(minimum) ||
                float.IsNaN(middle) || float.IsInfinity(middle) ||
                float.IsNaN(maximum) || float.IsInfinity(maximum) ||
                minimum <= 0.0f || minimum >= middle || middle >= maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum),
                    "Radial distances must be positive and strictly ordered minimum, middle, maximum.");
            }

            return new[] { minimum, middle, maximum };
        }
    }
}
