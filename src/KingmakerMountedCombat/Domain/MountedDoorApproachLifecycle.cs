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

    public sealed class MountedDoorApproachObservation
    {
        public double NowSeconds { get; set; }
        public bool InRange { get; set; }
        public MountedDoorApproachMoveState Move { get; set; }
        public string MoveResult { get; set; }
        public float DistanceToPoint { get; set; }
        public float DisplacementSinceLastTick { get; set; }
        public bool StallClockRuns { get; set; }
        // One uniform sample in [0, 1] for the native re-approach cooldown.
        public float ReapproachSample { get; set; }
    }

    public sealed class MountedDoorApproachLifecycle
    {
        private double nextApproachAt = double.NegativeInfinity;
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
                    if (o.NowSeconds < nextApproachAt) return MountedDoorApproachAction.Wait;
                    MoveStarts++;
                    if (!BeganAt.HasValue) BeganAt = o.NowSeconds;
                    ResetStall();
                    return MountedDoorApproachAction.BeginMove;
                case MountedDoorApproachMoveState.Finished:
                    return RecordApproachEnd("ended outside range (" + (o.MoveResult ?? "unknown") + ")", o.DistanceToPoint, o.NowSeconds, o.ReapproachSample)
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
            // A gap longer than one tick (turn-based rounds) is re-sampled, never charged.
            if (elapsed > MountedDoorApproachPolicy.MaximumTickGapSeconds)
            {
                return MountedDoorApproachAction.Wait;
            }
            stalledSeconds += Math.Max(0f, elapsed);
            if (!MountedDoorApproachPolicy.MovementStalled(stalledSeconds, MountedDoorApproachPolicy.StalledSecondsLimit, true))
            {
                return MountedDoorApproachAction.Wait;
            }
            return RecordApproachEnd("stalled for " + stalledSeconds.ToString("F1") + " game seconds", o.DistanceToPoint, o.NowSeconds, o.ReapproachSample)
                ? MountedDoorApproachAction.Exhausted : MountedDoorApproachAction.Stalled;
        }

        // True when the approach is exhausted; otherwise the next approach is scheduled after the native cooldown.
        private bool RecordApproachEnd(string why, float distance, double now, float reapproachSample)
        {
            LastApproachEnd = why;
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
