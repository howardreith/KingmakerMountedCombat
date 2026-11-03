using System;

namespace KingmakerMountedCombat.Domain
{
    /// <summary>Mount-root coordinates only. Bone orientation never enters this position lease.</summary>
    // A rider parented with its world transform kept is already compensated for the mount's scale at
    // that moment. Only a later change of the mount's scale (native growth) still has to be undone.
    public static class MountScaleCompensation
    {
        public static PoseVector3 AnchorScale(PoseVector3 mountScaleAtAttach, PoseVector3 currentMountScale)
        {
            if (!mountScaleAtAttach.IsFinite || !currentMountScale.IsFinite || currentMountScale.X == 0f || currentMountScale.Y == 0f || currentMountScale.Z == 0f)
                throw new InvalidOperationException("Mount scale compensation requires finite nonzero scales.");
            return new PoseVector3(mountScaleAtAttach.X / currentMountScale.X, mountScaleAtAttach.Y / currentMountScale.Y, mountScaleAtAttach.Z / currentMountScale.Z);
        }
    }

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
