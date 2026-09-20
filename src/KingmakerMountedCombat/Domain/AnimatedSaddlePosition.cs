using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>Mount-root coordinates only. Bone orientation never enters this position lease.</summary>
    public sealed class AnimatedSaddlePosition
    {
        private PoseVector3 seatFromSource;
        private PoseVector3 anatomicalCorrection;
        public bool IsAcquired { get; private set; }

        public void Acquire(PoseVector3 source, PoseVector3 posedPelvis, PoseVector3 correction)
        {
            if (IsAcquired || !source.IsFinite || !posedPelvis.IsFinite || !correction.IsFinite ||
                correction.Magnitude > 0.5f)
            {
                throw new InvalidOperationException("Animated saddle requires a fresh finite bounded calibration.");
            }
            seatFromSource = posedPelvis - source;
            anatomicalCorrection = correction;
            IsAcquired = true;
        }

        public PoseVector3 Project(PoseVector3 animatedSource)
        {
            if (!IsAcquired || !animatedSource.IsFinite)
            {
                throw new InvalidOperationException("Animated saddle source or lease is unavailable.");
            }
            return animatedSource + seatFromSource + anatomicalCorrection;
        }

        public void Release()
        {
            IsAcquired = false;
            seatFromSource = new PoseVector3(0f, 0f, 0f);
            anatomicalCorrection = new PoseVector3(0f, 0f, 0f);
        }
    }
}
