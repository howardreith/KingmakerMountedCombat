using System;

namespace KingmakerMountedCombat.Domain
{
    // Sprint 2 (SPACE-01, review correction A). The post-interaction settlement's decision path, kept free of engine
    // types: the door's navmesh change must be observed settled (two consecutive frames) before any footprint sample
    // is taken or any recovery destination is searched. A deadline reached while the cut or graph update is still
    // pending ends the settlement as Unresolved: nothing is sampled, nothing is searched, no move is issued, and no
    // collision-safe completion is awarded. The door's own interaction result is recorded separately by the command.
    public enum MountedDoorSettlementStep
    {
        Waiting,
        Evaluate,
        Unresolved
    }

    public sealed class MountedDoorSettlementVerdict
    {
        public MountedDoorRecoveryDisposition Disposition { get; set; }
        public bool FootprintSampled { get; set; }
        public MountedFootprintSample Sample { get; set; }
        public bool CandidateSearched { get; set; }
        public bool CandidateAdmitted { get; set; }
        public bool MoveAuthorized => Disposition == MountedDoorRecoveryDisposition.Admitted;
        public bool CollisionSafe => Disposition == MountedDoorRecoveryDisposition.NotNeeded;
    }

    public sealed class MountedDoorSettlementLifecycle
    {
        private readonly float limitSeconds;
        private int settledFrame = -1;

        public MountedDoorSettlementLifecycle(float limitSeconds)
        {
            if (float.IsNaN(limitSeconds) || float.IsInfinity(limitSeconds) || limitSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(limitSeconds));
            }
            this.limitSeconds = limitSeconds;
        }

        public MountedDoorSettlementStep Step { get; private set; } = MountedDoorSettlementStep.Waiting;
        public double ElapsedSeconds { get; private set; }
        public bool Settled => Step == MountedDoorSettlementStep.Evaluate;

        // One observation per frame. Settled only when the navmesh reports settled on two consecutive frames.
        public MountedDoorSettlementStep Observe(bool navmeshSettled, double elapsedSeconds, int frame)
        {
            if (Step != MountedDoorSettlementStep.Waiting)
            {
                return Step;
            }
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            }
            ElapsedSeconds = elapsedSeconds;
            if (navmeshSettled)
            {
                if (settledFrame < 0)
                {
                    settledFrame = frame;
                    return Step;
                }
                if (frame <= settledFrame)
                {
                    return Step;
                }
                Step = MountedDoorSettlementStep.Evaluate;
                return Step;
            }
            settledFrame = -1;
            if (elapsedSeconds <= limitSeconds)
            {
                return Step;
            }
            Step = MountedDoorSettlementStep.Unresolved;
            return Step;
        }

        // The decision after the wait. The sampler and the candidate search are invoked only on a settled graph.
        public static MountedDoorSettlementVerdict Resolve(
            MountedDoorSettlementStep step,
            Func<MountedFootprintSample> sampleFootprint,
            Func<bool> admitRecoveryCandidate,
            bool turnBasedCombat,
            bool riderTurnActing,
            bool mountIdle)
        {
            if (sampleFootprint == null) throw new ArgumentNullException(nameof(sampleFootprint));
            if (admitRecoveryCandidate == null) throw new ArgumentNullException(nameof(admitRecoveryCandidate));
            switch (step)
            {
                case MountedDoorSettlementStep.Unresolved:
                    return new MountedDoorSettlementVerdict { Disposition = MountedDoorRecoveryDisposition.Unresolved };
                case MountedDoorSettlementStep.Evaluate:
                    var sample = sampleFootprint();
                    var verdict = new MountedDoorSettlementVerdict { FootprintSampled = true, Sample = sample };
                    if (sample.IsClear)
                    {
                        verdict.Disposition = MountedDoorRecoveryDisposition.NotNeeded;
                        return verdict;
                    }
                    verdict.CandidateSearched = true;
                    verdict.CandidateAdmitted = admitRecoveryCandidate();
                    verdict.Disposition = MountedDoorOccupancyPolicy.DecideRecovery(false, verdict.CandidateAdmitted, turnBasedCombat, riderTurnActing, mountIdle);
                    return verdict;
                default:
                    throw new InvalidOperationException("The settlement is still waiting for the navmesh.");
            }
        }
    }
}
