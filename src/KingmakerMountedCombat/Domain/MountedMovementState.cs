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
            singleMove = EffectiveSingleMove(singleMove, standardUsed);
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

        // Review R7-F4: the single-action movement limit exists only while the Standard is unused. Installed contract
        // (Test-MmrTbApproachNativeContract.ps1): TurnController.TrySelectMovementLimit sets the SingleAction limit back to None
        // when the unit has used its Standard action, so a native unit never moves under "single move with Standard spent".
        // A paired mount can receive the rider's single-move choice while its own Standard is spent; it then moves with its
        // ordinary allowance (its one remaining Move action), not with a limit native would have cleared.
        public static bool EffectiveSingleMove(bool singleMove, bool standardUsed) => singleMove && !standardUsed;

        public float Remaining(float speed, float stepRange, float moveUsed, bool standardUsed,
            bool moveRestricted, bool step, bool singleMove)
        {
            singleMove = EffectiveSingleMove(singleMove, standardUsed);
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
        // Reviews R6-F3, R7-F4, PR #70 R1-F6: the time available across one order, computed by DRY-RUNNING this state's own Advance
        // on an isolated copy (the live state, cooldowns and actions are never touched) with the real restriction, step, single-move,
        // Standard and auto-stop inputs. Execution debits movement tick by tick (KMC TickDelegated, like installed
        // TurnController.TickMovement, which re-reads GetRemainingMovementTime(false, EnabledSingleActionMove) before every tick and
        // clamps the tick to it), so a rule that depends on movement already spent applies after the FIRST tick: with the single-action
        // limit and three seconds or less remaining (a restricted Move), native ends the movement as soon as MoveAction is positive.
        // The dry run therefore spends one FirstTickSeconds tick first (not counted: no order relies on it) and then whole slices,
        // which land exactly on each native threshold (Remaining returns the time to the next one). The result never exceeds what
        // successive ordinary ticks grant.
        public const float FirstTickSeconds = 0.001f;
        public float RemainingAcrossOrder(float speed, float stepRange, float moveUsed, bool standardUsed, bool moveRestricted,
            bool step, bool singleMove, bool autoStop)
        {
            var copy = new MountedMovementState
            {
                TimeMoved = TimeMoved, TimeForced = TimeForced, TimeStepped = TimeStepped, MetresStepped = MetresStepped,
                StepImmune = StepImmune, AiStep = AiStep, AutoStopPending = AutoStopPending
            };
            var total = 0f;
            var used = moveUsed;
            float firstDebit;
            if (!(copy.Advance(FirstTickSeconds, speed, stepRange, used, standardUsed, moveRestricted, step, singleMove, false, false, autoStop, out firstDebit) > 0f))
                return 0f;
            used += firstDebit;
            for (var slice = 0; slice < 8; slice++)
            {
                float debit;
                var granted = copy.Advance(6f, speed, stepRange, used, standardUsed, moveRestricted, step, singleMove, false, false, autoStop, out debit);
                if (!(granted > 0f)) break;
                total += granted;
                used += debit;
            }
            return total;
        }

        private static bool Finite(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
