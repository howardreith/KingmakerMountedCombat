using System;

namespace KingmakerMountedCombat.Domain
{
    // Review R6-F2. Unit yaw in Kingmaker/Unity convention (UnitEntityData.Orientation, degrees): 0 faces +Z, 90 faces +X. A
    // rider carried at a mount-local offset turns with the mount, so an offset measured now (in the mount's current yaw) is
    // rotated by the turn from that yaw to the heading the mount will have at a route point (its travel direction there).
    public static class MountedHeading
    {
        // The yaw of a horizontal travel vector; NaN when the vector has no horizontal length (no heading change).
        public static float YawDegrees(float x, float z) =>
            x * x + z * z <= 1e-12f ? float.NaN : (float)(Math.Atan2(x, z) * 180d / Math.PI);

        // The turn (degrees, -180..180) from the current yaw to the travel direction; 0 for a zero travel vector.
        public static float TurnDegrees(float currentYawDegrees, float travelX, float travelZ)
        {
            var target = YawDegrees(travelX, travelZ);
            if (float.IsNaN(target) || float.IsNaN(currentYawDegrees)) return 0f;
            var turn = (target - currentYawDegrees) % 360f;
            if (turn > 180f) turn -= 360f;
            if (turn < -180f) turn += 360f;
            return turn;
        }

        // Rotates a horizontal offset about the vertical axis by the given yaw change (Unity: Quaternion.Euler(0, degrees, 0)).
        public static void Rotate(float x, float z, float degrees, out float rotatedX, out float rotatedZ)
        {
            var radians = degrees * Math.PI / 180d;
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);
            rotatedX = (float)(x * cos + z * sin);
            rotatedZ = (float)(-x * sin + z * cos);
        }
    }
}
