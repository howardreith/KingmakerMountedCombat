using System;

namespace KingmakerMountedCombat.Domain
{
    public struct PoseVector3 : IEquatable<PoseVector3>
    {
        public PoseVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        public float SqrMagnitude => (X * X) + (Y * Y) + (Z * Z);

        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public bool IsFinite => IsFiniteValue(X) && IsFiniteValue(Y) && IsFiniteValue(Z);

        public PoseVector3 Normalized
        {
            get
            {
                var magnitude = Magnitude;
                return magnitude <= 0.000001f ? new PoseVector3(0f, 0f, 0f) : this / magnitude;
            }
        }

        public static PoseVector3 operator +(PoseVector3 first, PoseVector3 second)
        {
            return new PoseVector3(first.X + second.X, first.Y + second.Y, first.Z + second.Z);
        }

        public static PoseVector3 operator -(PoseVector3 first, PoseVector3 second)
        {
            return new PoseVector3(first.X - second.X, first.Y - second.Y, first.Z - second.Z);
        }

        public static PoseVector3 operator *(PoseVector3 value, float scalar)
        {
            return new PoseVector3(value.X * scalar, value.Y * scalar, value.Z * scalar);
        }

        public static PoseVector3 operator /(PoseVector3 value, float scalar)
        {
            return new PoseVector3(value.X / scalar, value.Y / scalar, value.Z / scalar);
        }

        public static float Dot(PoseVector3 first, PoseVector3 second)
        {
            return (first.X * second.X) + (first.Y * second.Y) + (first.Z * second.Z);
        }

        public static PoseVector3 Cross(PoseVector3 first, PoseVector3 second)
        {
            return new PoseVector3(
                (first.Y * second.Z) - (first.Z * second.Y),
                (first.Z * second.X) - (first.X * second.Z),
                (first.X * second.Y) - (first.Y * second.X));
        }

        public static float Distance(PoseVector3 first, PoseVector3 second)
        {
            return (first - second).Magnitude;
        }

        public bool Equals(PoseVector3 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override bool Equals(object obj)
        {
            return obj is PoseVector3 && Equals((PoseVector3)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + X.GetHashCode();
                hash = (hash * 31) + Y.GetHashCode();
                hash = (hash * 31) + Z.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }

        private static bool IsFiniteValue(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
