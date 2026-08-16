using System;

namespace KingmakerMountedCombat.Domain
{
    public struct MountedCombatPoint
    {
        public MountedCombatPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }

        public float Z { get; }
    }

    public static class MountedCombatSpatialPolicy
    {
        public const float RangeTolerance = 0.05f;

        public static float CalculateStoppingRadius(
            float mammothCorpulence,
            float targetCorpulence,
            float weaponRange)
        {
            RequireFiniteNonNegative(mammothCorpulence, nameof(mammothCorpulence));
            RequireFiniteNonNegative(targetCorpulence, nameof(targetCorpulence));
            RequireFiniteNonNegative(weaponRange, nameof(weaponRange));
            return mammothCorpulence + targetCorpulence + weaponRange;
        }

        public static bool IsWithinRange(
            MountedCombatPoint mammothOrigin,
            MountedCombatPoint targetOrigin,
            float stoppingRadius)
        {
            RequireFiniteNonNegative(stoppingRadius, nameof(stoppingRadius));
            RequireFinite(mammothOrigin.X, nameof(mammothOrigin));
            RequireFinite(mammothOrigin.Z, nameof(mammothOrigin));
            RequireFinite(targetOrigin.X, nameof(targetOrigin));
            RequireFinite(targetOrigin.Z, nameof(targetOrigin));
            var dx = mammothOrigin.X - targetOrigin.X;
            var dz = mammothOrigin.Z - targetOrigin.Z;
            var admitted = stoppingRadius + RangeTolerance;
            return (dx * dx) + (dz * dz) <= admitted * admitted;
        }

        private static void RequireFiniteNonNegative(float value, string name)
        {
            RequireFinite(value, name);
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        private static void RequireFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }

    public static class MountedPairTurnPolicy
    {
        public static bool ShouldEndMountTurn(
            bool exactMountedPair,
            bool turnBasedCombat,
            bool currentUnitIsExactMount)
        {
            return exactMountedPair && turnBasedCombat && currentUnitIsExactMount;
        }

        public static bool CanDelegateMountMovement(
            bool exactMountedPair,
            bool turnBasedCombat,
            bool currentUnitIsExactRider,
            bool riderTurnIsActing,
            bool movingAgentIsExactMount)
        {
            return exactMountedPair &&
                turnBasedCombat &&
                currentUnitIsExactRider &&
                riderTurnIsActing &&
                movingAgentIsExactMount;
        }
    }
}
