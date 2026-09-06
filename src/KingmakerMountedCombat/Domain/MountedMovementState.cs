using System;

namespace KingmakerMountedCombat.Domain
{
    // Movement-only state. Costs stay in the native mount cooldown object;
    // this retains the off-executor turn fields that Kingmaker otherwise loses.
    public sealed class MountedMovementState
    {
        public float TimeMoved;
        public float TimeForced;
        public float TimeStepped;
        public float MetresStepped;
        public bool StepImmune;
        public bool AiStep;
        public bool AutoStopPending;

        public float Advance(float requested, float speed, float stepRange, float moveUsed,
            bool standardUsed, bool moveRestricted, bool step, bool singleMove, bool forced,
            bool nearEnd, bool autoStop, out float moveDebit)
        {
            moveDebit = 0f;
            if (!Finite(requested) || !Finite(speed) || !Finite(stepRange) || !Finite(moveUsed))
                throw new ArgumentOutOfRangeException("Mounted movement requires finite nonnegative native state.");
            if (forced)
            {
                TimeMoved += requested;
                TimeForced += requested;
                return requested;
            }
            var remaining = Remaining(speed, stepRange, moveUsed, standardUsed, moveRestricted, step, singleMove);
            // Native NearTheEnd permits its final small endpoint correction. It
            // never authorizes another request after the allowance is exhausted.
            var allowed = remaining <= 0f ? 0f : nearEnd ? requested : Math.Min(requested, remaining);
            if (AutoStopPending)
            {
                AutoStopPending = false;
                return 0f;
            }
            TimeMoved += allowed;
            if (step)
            {
                TimeStepped += allowed;
                MetresStepped += allowed * speed;
                StepImmune |= allowed > 0f;
            }
            else
            {
                moveDebit = allowed;
                if (autoStop && !singleMove && moveUsed < 3f && moveUsed + allowed >= 3f)
                    AutoStopPending = true;
            }
            return allowed;
        }

        public float Remaining(float speed, float stepRange, float moveUsed, bool standardUsed,
            bool moveRestricted, bool step, bool singleMove)
        {
            if (step)
            {
                if (TimeMoved > TimeStepped || speed <= 0f || speed * 3f <= stepRange) return 0f;
                return Math.Max(0f, stepRange - MetresStepped) / speed;
            }
            if ((!AiStep && MetresStepped > 0f) || TimeForced > 0f) return 0f;
            // Exact Kingmaker GetRemainingTime/GetRemainingMovementTime formula.
            // MoveAction > 3 is native Standard conversion, not a second grant.
            var remaining = Math.Max(0f, Math.Min(6f,
                3f * ((moveRestricted ? 1f : 2f) - (standardUsed ? 1f : 0f)) - moveUsed));
            if (remaining > 3f) return remaining - 3f;
            return singleMove && moveUsed > 0f ? 0f : remaining;
        }
        private static bool Finite(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
