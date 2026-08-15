using System;

namespace KingmakerMountedCombat.Domain
{
    public sealed class TwoBonePoseSolution
    {
        internal TwoBonePoseSolution(PoseVector3 joint, PoseVector3 target, float requestedDistance, float solvedDistance, bool targetClamped)
        {
            Joint = joint;
            Target = target;
            RequestedDistance = requestedDistance;
            SolvedDistance = solvedDistance;
            TargetClamped = targetClamped;
        }

        public PoseVector3 Joint { get; }

        public PoseVector3 Target { get; }

        public float RequestedDistance { get; }

        public float SolvedDistance { get; }

        public bool TargetClamped { get; }
    }

    public static class TwoBonePoseSolver
    {
        private const float Epsilon = 0.0001f;

        public static TwoBonePoseSolution Solve(
            PoseVector3 root,
            PoseVector3 requestedTarget,
            PoseVector3 bendHint,
            float firstSegmentLength,
            float secondSegmentLength)
        {
            if (!root.IsFinite || !requestedTarget.IsFinite || !bendHint.IsFinite)
            {
                throw new ArgumentException("Two-bone pose inputs must be finite.");
            }
            if (!IsFinitePositive(firstSegmentLength) || !IsFinitePositive(secondSegmentLength))
            {
                throw new ArgumentOutOfRangeException("Two-bone segment lengths must be finite and positive.");
            }

            var requestedDelta = requestedTarget - root;
            var requestedDistance = requestedDelta.Magnitude;
            var direction = requestedDistance > Epsilon
                ? requestedDelta / requestedDistance
                : ResolveFallbackDirection(bendHint - root);
            var minimumDistance = Math.Abs(firstSegmentLength - secondSegmentLength) + Epsilon;
            var maximumDistance = Math.Max(minimumDistance, firstSegmentLength + secondSegmentLength - Epsilon);
            var solvedDistance = Math.Max(minimumDistance, Math.Min(maximumDistance, requestedDistance));
            var target = root + (direction * solvedDistance);

            var hintDelta = bendHint - root;
            var bendDirection = hintDelta - (direction * PoseVector3.Dot(hintDelta, direction));
            if (bendDirection.SqrMagnitude <= Epsilon * Epsilon)
            {
                bendDirection = DeterministicPerpendicular(direction);
            }
            else
            {
                bendDirection = bendDirection.Normalized;
            }

            var along = ((firstSegmentLength * firstSegmentLength) - (secondSegmentLength * secondSegmentLength) +
                (solvedDistance * solvedDistance)) / (2f * solvedDistance);
            var heightSquared = Math.Max(0f, (firstSegmentLength * firstSegmentLength) - (along * along));
            var height = (float)Math.Sqrt(heightSquared);
            var joint = root + (direction * along) + (bendDirection * height);
            var clamped = Math.Abs(solvedDistance - requestedDistance) > Epsilon;
            return new TwoBonePoseSolution(joint, target, requestedDistance, solvedDistance, clamped);
        }

        private static PoseVector3 ResolveFallbackDirection(PoseVector3 hintDelta)
        {
            if (hintDelta.SqrMagnitude > Epsilon * Epsilon)
            {
                return hintDelta.Normalized;
            }
            return new PoseVector3(0f, -1f, 0f);
        }

        private static PoseVector3 DeterministicPerpendicular(PoseVector3 direction)
        {
            var reference = Math.Abs(direction.Y) < 0.9f
                ? new PoseVector3(0f, 1f, 0f)
                : new PoseVector3(1f, 0f, 0f);
            return PoseVector3.Cross(direction, reference).Normalized;
        }

        private static bool IsFinitePositive(float value)
        {
            return value > Epsilon && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
