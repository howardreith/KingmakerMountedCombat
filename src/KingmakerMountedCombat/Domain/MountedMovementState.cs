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
        // The installed step counter (TurnController.MetersMovedByFiveFootStep): nominal units, CurrentSpeedMps x time (PR #70 R9-F3, round 11).
        public float MetresStepped;
        // PR #70 R9-F3 (round 11): the step's PHYSICAL travel (the steered displacement). Advance reserves this tick's bound (speed x time) and
        // the movement adapter replaces the reservation with the displacement actually executed once it is observed (SettleStepTravel).
        public float MetresStepTravelled;
        public bool StepImmune;
        public bool AiStep;
        public bool AutoStopPending;

        // Forced (charge) travel closes ordinary locomotion for the rest of the allocation's activation. Known issues
        // Sprint 7 (U01): the movement slots' presentation reads this same fact.
        public bool ForcedTravelRecorded => TimeForced > 0f;

        public float Advance(float requested, float speed, float stepRange, float moveUsed,
            bool standardUsed, bool moveRestricted, bool step, bool singleMove, bool forced,
            bool nearEnd, bool autoStop, out float moveDebit) =>
            Advance(requested, speed, speed, stepRange, moveUsed, standardUsed, moveRestricted, step, singleMove, forced, nearEnd, autoStop, out moveDebit);

        // PR #70 R9-F3 (round 11): speed is the mover's physical speed (the agent's MaxSpeed: the distance one second of movement shifts it);
        // nominalSpeed the unit's CurrentSpeedMps, the unit in which the installed TurnController.TickMovement charges a step
        // (MetersMovedByFiveFootStep += CurrentSpeedMps x delta). A step is bounded by both: the native counter and the physical travel.
        public float Advance(float requested, float speed, float nominalSpeed, float stepRange, float moveUsed,
            bool standardUsed, bool moveRestricted, bool step, bool singleMove, bool forced,
            bool nearEnd, bool autoStop, out float moveDebit)
        {
            moveDebit = 0f;
            singleMove = EffectiveSingleMove(singleMove, standardUsed);
            if (!Finite(requested) || !Finite(speed) || !Finite(nominalSpeed) || !Finite(stepRange) || !Finite(moveUsed))
                throw new ArgumentOutOfRangeException("Mounted movement requires finite nonnegative native state.");
            if (forced)
            {
                TimeMoved += requested;
                TimeForced += requested;
                return requested;
            }
            var remaining = Remaining(speed, nominalSpeed, stepRange, moveUsed, standardUsed, moveRestricted, step, singleMove);
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
                MetresStepped += allowed * nominalSpeed;
                MetresStepTravelled += allowed * speed;
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
            bool moveRestricted, bool step, bool singleMove) =>
            Remaining(speed, speed, stepRange, moveUsed, standardUsed, moveRestricted, step, singleMove);

        // A step's remaining time is the smaller of the native counter's (nominal metres left / nominal speed) and the physical travel's
        // (metres left / physical speed): a slower override cannot spend past the native counter, a faster one cannot travel past the step.
        public float Remaining(float speed, float nominalSpeed, float stepRange, float moveUsed, bool standardUsed,
            bool moveRestricted, bool step, bool singleMove)
        {
            singleMove = EffectiveSingleMove(singleMove, standardUsed);
            if (step)
            {
                if (TimeMoved > TimeStepped || speed <= 0f || nominalSpeed <= 0f || nominalSpeed * 3f <= stepRange) return 0f;
                return Math.Min(Math.Max(0f, stepRange - MetresStepped) / nominalSpeed, Math.Max(0f, stepRange - MetresStepTravelled) / speed);
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
        // PR #70 R2-F1/R1-F6 (round 3): whole slices land exactly on each native threshold (Remaining returns the time to the next
        // one), so the dry run equals successive ordinary ticks for every state except one: the single-action limit with three seconds
        // or less remaining and no Move spent yet (a restricted Move), where native grants exactly the FIRST tick, whose length is the
        // engine frame's and cannot be known before execution. FirstTickOnly names that state; the order allowance is 0 there, and
        // click admission keeps native's own decision for it (KMC never authorizes travel whose length depends on a frame). Every other
        // allowance (steps, ordinary movement, a spent Standard, the lawful conversion) is the full executed amount.
        public bool FirstTickOnly(float moveUsed, bool standardUsed, bool moveRestricted, bool step, bool singleMove)
        {
            if (step || !EffectiveSingleMove(singleMove, standardUsed) || moveUsed > 0f) return false;
            if ((!AiStep && MetresStepped > 0f) || ForcedTravelRecorded) return false;
            var remaining = Math.Max(0f, Math.Min(6f, 3f * ((moveRestricted ? 1f : 2f) - (standardUsed ? 1f : 0f)) - moveUsed));
            return remaining > 0f && remaining <= 3f;
        }
        public float RemainingAcrossOrder(float speed, float stepRange, float moveUsed, bool standardUsed, bool moveRestricted,
            bool step, bool singleMove, bool autoStop) =>
            RemainingAcrossOrder(speed, speed, stepRange, moveUsed, standardUsed, moveRestricted, step, singleMove, autoStop);

        public float RemainingAcrossOrder(float speed, float nominalSpeed, float stepRange, float moveUsed, bool standardUsed, bool moveRestricted,
            bool step, bool singleMove, bool autoStop)
        {
            var copy = new MountedMovementState
            {
                TimeMoved = TimeMoved, TimeForced = TimeForced, TimeStepped = TimeStepped, MetresStepped = MetresStepped, MetresStepTravelled = MetresStepTravelled,
                StepImmune = StepImmune, AiStep = AiStep, AutoStopPending = AutoStopPending
            };
            if (FirstTickOnly(moveUsed, standardUsed, moveRestricted, step, singleMove)) return 0f;
            var total = 0f;
            var used = moveUsed;
            for (var slice = 0; slice < 8; slice++)
            {
                float debit;
                var granted = copy.Advance(6f, speed, nominalSpeed, stepRange, used, standardUsed, moveRestricted, step, singleMove, false, false, autoStop, out debit);
                if (!(granted > 0f)) break;
                total += granted;
                used += debit;
            }
            return total;
        }

        private static bool Finite(float value) => value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
