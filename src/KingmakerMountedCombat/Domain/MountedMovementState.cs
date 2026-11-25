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

        // Forced (charge) travel closes ordinary locomotion for the rest of the allocation's activation. Known issues
        // Sprint 7 (U01): the movement slots' presentation reads this same fact.
        public bool ForcedTravelRecorded => TimeForced > 0f;

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
            if ((!AiStep && MetresStepped > 0f) || ForcedTravelRecorded) return 0f;
            // Exact Kingmaker GetRemainingTime/GetRemainingMovementTime formula.
            // MoveAction > 3 is native Standard conversion, not a second grant.
            var remaining = Math.Max(0f, Math.Min(6f,
                3f * ((moveRestricted ? 1f : 2f) - (standardUsed ? 1f : 0f)) - moveUsed));
            if (remaining > 3f) return remaining - 3f;
            return singleMove && moveUsed > 0f ? 0f : remaining;
        }
        // Review R6-F3: the time available across one order, as successive Advance calls of this same state would grant it.
        // Remaining is only the current action's slice (native GetRemainingMovementTime(total: false)); an ordinary
        // two-action move continues into the Standard conversion (the mount pays it, moveUsed passing 3) unless the
        // single-move limit, a five-foot step, auto-stop after the first Move action, a restriction or a spent Standard ends it.
        public float RemainingAcrossOrder(float speed, float stepRange, float moveUsed, bool standardUsed, bool moveRestricted,
            bool step, bool singleMove, bool autoStop)
        {
            var current = Remaining(speed, stepRange, moveUsed, standardUsed, moveRestricted, step, singleMove);
            if (step || singleMove || current <= 0f) return current;
            if (AutoStopPending) return 0f;
            var total = Math.Max(0f, Math.Min(6f, 3f * ((moveRestricted ? 1f : 2f) - (standardUsed ? 1f : 0f)) - moveUsed));
            if (autoStop && moveUsed < 3f) return Math.Min(total, 3f - moveUsed);
            return total;
        }

        private static bool Finite(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
