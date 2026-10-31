using System;

namespace KingmakerMountedCombat.Domain
{
    // Sprint 2 (DOOR-01). The routed door command's approach state machine, kept free of engine types so the
    // lifecycle can be driven offline across the native nine-second boundary. The command stays UNSTARTED (CanStart
    // false) until this lifecycle reports Satisfied at valid arrival, however long the mount's delegated move takes;
    // the native "longer than 1 round" interrupt applies only to started commands, so it cannot end the approach.
    public enum MountedDoorApproachMoveState
    {
        None,
        Live,
        Finished
    }

    public enum MountedDoorApproachAction
    {
        Wait,
        BeginMove,
        Satisfied,
        Retry,
        Stalled,
        Exhausted
    }

    // Sprint 2 (review 2026-10-31, turn-based clock). The supervision clock behind the door approach, the settlement wait
    // and the recovery stall. Native turn-based GameTime is rewound to the turn's start on every frame of an acting turn
    // (CombatController.TickTime), so a cooldown or a stall bound measured in campaign seconds never elapses inside a
    // turn: a finished approach scheduled "0.4 s later" waited forever, a still mount accrued no stall time, a pending
    // graph update could not reach the settlement deadline. This clock accumulates the engine's own per-frame gameplay
    // timestep (TimeController.GameDeltaTime: zero while paused or outside gameplay modes, the movement timestep inside
    // a turn) on exactly the frames the supervised operation was ticked. It measures supervision only: it never
    // advances game time, grants movement, refunds anything or is itself a movement charge, and a frame the operation
    // was not ticked (off-turn, paused) adds nothing, so resuming never charges the waiting gap.
    public sealed class MountedDoorOperationalClock
    {
        private int lastFrame = int.MinValue;

        public double Seconds { get; private set; }
        public int ObservedFrames { get; private set; }
        public int SkippedSteps { get; private set; }

        // One observation per frame; a repeated observation in the same frame adds nothing. A non-finite, non-positive
        // or oversized step (an engine hitch, a resumed process) is skipped, never charged.
        public double Observe(int frame, float gameplayDeltaSeconds)
        {
            if (frame == lastFrame) return Seconds;
            lastFrame = frame;
            ObservedFrames++;
            if (float.IsNaN(gameplayDeltaSeconds) || float.IsInfinity(gameplayDeltaSeconds) || gameplayDeltaSeconds <= 0f) return Seconds;
            if (gameplayDeltaSeconds > MountedDoorApproachPolicy.MaximumTickGapSeconds) { SkippedSteps++; return Seconds; }
            Seconds += gameplayDeltaSeconds;
            return Seconds;
        }
    }

    public sealed class MountedDoorApproachObservation
    {
        // Operational seconds (MountedDoorOperationalClock), never campaign GameTime.
        public double NowSeconds { get; set; }
        public bool InRange { get; set; }
        public MountedDoorApproachMoveState Move { get; set; }
        public string MoveResult { get; set; }
        public float DistanceToPoint { get; set; }
        public float DisplacementSinceLastTick { get; set; }
        public bool StallClockRuns { get; set; }
        // One uniform sample in [0, 1] for the native re-approach cooldown.
        public float ReapproachSample { get; set; }
        // Turn-based combat only: an opaque token identifying the acting turn this tick belongs to (null in real time).
        // An approach that ended on a turn is re-attempted on a later turn, never on the same one: the mount's movement
        // allocation for the turn is spent or the route failed, and the native re-approach cooldown (campaign time,
        // frozen inside a turn) defers the native rider approach the same way. Nothing here grants or spends movement.
        public object TurnToken { get; set; }
    }

    public sealed class MountedDoorApproachLifecycle
    {
        private double nextApproachAt = double.NegativeInfinity;
        private object approachEndedOnTurn;
        private double lastStallSampleAt = double.NaN;
        private bool stallSampled;
        private float stalledSeconds;

        public bool Satisfied { get; private set; }
        public int MoveStarts { get; private set; }
        public int ApproachesWithoutProgress { get; private set; }
        public float BestDistance { get; private set; } = float.PositiveInfinity;
        public double? BeganAt { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public float StalledSeconds => stalledSeconds;
        public string ExhaustedReason { get; private set; }
        public string LastApproachEnd { get; private set; }

        public MountedDoorApproachAction Tick(MountedDoorApproachObservation o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            if (Satisfied) return MountedDoorApproachAction.Satisfied;
            if (ExhaustedReason != null) return MountedDoorApproachAction.Exhausted;
            if (o.InRange)
            {
                Satisfied = true;
                ElapsedSeconds = BeganAt.HasValue ? (float)(o.NowSeconds - BeganAt.Value) : 0f;
                return MountedDoorApproachAction.Satisfied;
            }
            switch (o.Move)
            {
                case MountedDoorApproachMoveState.None:
                    // Turn-based: the turn on which the last approach ended never re-attempts; a later turn does.
                    if (approachEndedOnTurn != null && o.TurnToken != null && ReferenceEquals(approachEndedOnTurn, o.TurnToken)) return MountedDoorApproachAction.Wait;
                    if (o.NowSeconds < nextApproachAt) return MountedDoorApproachAction.Wait;
                    approachEndedOnTurn = null;
                    MoveStarts++;
                    if (!BeganAt.HasValue) BeganAt = o.NowSeconds;
                    ResetStall();
                    return MountedDoorApproachAction.BeginMove;
                case MountedDoorApproachMoveState.Finished:
                    return RecordApproachEnd("ended outside range (" + (o.MoveResult ?? "unknown") + ")", o.DistanceToPoint, o.NowSeconds, o.ReapproachSample, o.TurnToken)
                        ? MountedDoorApproachAction.Exhausted : MountedDoorApproachAction.Retry;
                default:
                    return ObserveStall(o);
            }
        }

        private MountedDoorApproachAction ObserveStall(MountedDoorApproachObservation o)
        {
            if (!stallSampled || !o.StallClockRuns)
            {
                stallSampled = true;
                lastStallSampleAt = o.NowSeconds;
                return MountedDoorApproachAction.Wait;
            }
            var elapsed = (float)(o.NowSeconds - lastStallSampleAt);
            lastStallSampleAt = o.NowSeconds;
            if (o.DisplacementSinceLastTick >= MountedDoorApproachPolicy.DisplacementEpsilonMetres)
            {
                stalledSeconds = 0f;
                return MountedDoorApproachAction.Wait;
            }
            // A step longer than one tick is re-sampled, never charged (the operational clock produces none; a guard).
            if (elapsed > MountedDoorApproachPolicy.MaximumTickGapSeconds)
            {
                return MountedDoorApproachAction.Wait;
            }
            stalledSeconds += Math.Max(0f, elapsed);
            if (!MountedDoorApproachPolicy.MovementStalled(stalledSeconds, MountedDoorApproachPolicy.StalledSecondsLimit, true))
            {
                return MountedDoorApproachAction.Wait;
            }
            return RecordApproachEnd("stalled for " + stalledSeconds.ToString("F1") + " operational seconds", o.DistanceToPoint, o.NowSeconds, o.ReapproachSample, o.TurnToken)
                ? MountedDoorApproachAction.Exhausted : MountedDoorApproachAction.Stalled;
        }

        // True when the approach is exhausted; otherwise the next approach is scheduled after the native cooldown
        // (and, in turn-based combat, no earlier than a later turn).
        private bool RecordApproachEnd(string why, float distance, double now, float reapproachSample, object turnToken)
        {
            LastApproachEnd = why;
            approachEndedOnTurn = turnToken;
            if (MountedDoorApproachPolicy.ApproachProgressed(BestDistance, distance, MountedDoorApproachPolicy.ProgressEpsilonMetres))
            {
                BestDistance = distance;
                ApproachesWithoutProgress = 0;
            }
            else
            {
                ApproachesWithoutProgress++;
            }
            ResetStall();
            if (MountedDoorApproachPolicy.ApproachExhausted(ApproachesWithoutProgress, MountedDoorApproachPolicy.MaximumApproachesWithoutProgress))
            {
                ExhaustedReason = "Mount door approach cannot reach the exact interaction point: " + ApproachesWithoutProgress +
                    " approaches " + why + " without progress; nearest " + BestDistance.ToString("F2") + " m.";
                return true;
            }
            nextApproachAt = now + MountedDoorApproachPolicy.ReapproachDelaySeconds(reapproachSample);
            return false;
        }

        private void ResetStall()
        {
            stallSampled = false;
            stalledSeconds = 0f;
            lastStallSampleAt = double.NaN;
        }
    }
}
