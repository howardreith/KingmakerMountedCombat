using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // MMR-01 (docs/missions/mounted-alpha2-melee-pose/MISSION.md; reviews R1-F12, R5-F3, R5-F4). The animated saddle keeps the
    // rider's pelvis at source + seatFromSource + correction every frame, where source is the Horse Chest and every vector is in
    // the Horse mount root's local frame (metres; X anatomical right, Y up, Z forward).
    //
    // The basis is the rider's OWN settled pose in a NORMALIZED frame (review R5-F3). Production places the rider position
    // anchor once, from the Chest as it stood in the attachment frame (KingmakerMountedPairRuntime.AttachPresentation:
    // Chest.TransformPoint(SourceAnchorOffset) + MountRootPositionOffset, fixed under the mount root), and RiderMovementAgent
    // keeps the rider root on that anchor. A raw pelvis-minus-current-Chest sample therefore carries the Chest's displacement
    // at attachment. The normalized sample is instead
    //   seatFromSource = (anchor - Chest), both taken in the attachment frame (the declared anchor relation: for the Horse
    //                    exactly MountRootPositionOffset), plus
    //                    (pelvis - rider root) in the current frame (the rider's own rig pose, rider-local),
    // so two attachments made with the Chest at different heights settle on the same pelvis relative to the Chest once the
    // rigs reach the same idle geometry. Mechanics (anchor, rider root, entity position) are not touched.
    //
    // Settling (review R5-F4) counts only frames in which the rider's animation actually advanced (its animation manager
    // enabled and not disabled, and a positive game delta: Kingmaker's pause suspends Time.timeScale), and requires the sample
    // to stay within StableMetres of the window's FIRST sample for StableWindowSeconds of animation time, after at least
    // MinimumAnimationSeconds of advancing animation (so a cast clip held under pause, or a pelvis drifting slowly, never
    // settles). A paused or unevaluated period leaves the provisional sample eligible for correction when animation resumes.
    // HorseAnimatedSeatCorrection (the accepted +0.19/-0.16/-0.18 visual correction) is added relative to the basis. Every
    // attachment logs the attachment and the settled sample (FormatNotice). Mammoth has no animated saddle; Small profiles keep
    // their fixed-from-source seat.
    public static class AnimatedSaddleCalibration
    {
        // The normalized seat sample: the attachment-frame anchor relation plus the rider's current rider-local pelvis.
        public static PoseVector3 NormalizedSeatFromSource(PoseVector3 anchorFromSourceAtAttach, PoseVector3 pelvis, PoseVector3 riderRoot) =>
            anchorFromSourceAtAttach + (pelvis - riderRoot);

        public static string FormatNotice(string phase, string riderId, PoseVector3 observedSeatFromSource, PoseVector3 appliedSeatFromSource,
            PoseVector3 seatCorrection, int frames, float animationSeconds, PoseVector3 anchorFromSource) =>
            "Horse seatCalibration: phase=" + phase + "; rider=" + (riderId ?? "<none>") +
            "; observedSeatFromSource=" + Format(observedSeatFromSource) + "; appliedSeatFromSource=" + Format(appliedSeatFromSource) +
            "; anchorFromSource=" + Format(anchorFromSource) + "; seatCorrection=" + Format(seatCorrection) + "; frames=" + frames +
            "; animationSeconds=" + animationSeconds.ToString("F3", CultureInfo.InvariantCulture) + ".";

        public static string Format(PoseVector3 value) =>
            "(" + value.X.ToString("F4", CultureInfo.InvariantCulture) + "," + value.Y.ToString("F4", CultureInfo.InvariantCulture) + "," +
            value.Z.ToString("F4", CultureInfo.InvariantCulture) + ")";
    }

    // The settle decision for one attachment, in animation time (review R5-F4).
    public sealed class SettledSeatSampler
    {
        // Longer than the Mount Companion cast and an approach walk's blend, measured only while animation advances.
        public const float MinimumAnimationSeconds = 2f;
        public const float StableWindowSeconds = 0.5f;
        public const float StableMetres = 0.002f;
        public const float MaximumAnimationSeconds = 30f;

        private PoseVector3 windowStart;
        private bool hasWindow;
        private float windowSeconds;

        // Frames with advancing animation that were sampled; frames without advance are counted separately and change nothing.
        public int Frames { get; private set; }
        public int IgnoredFrames { get; private set; }
        public float AnimationSeconds { get; private set; }
        public bool Settled { get; private set; }
        public bool GaveUp { get; private set; }
        public int FramesToSettle { get; private set; }
        public float AnimationSecondsToSettle { get; private set; }
        public PoseVector3 SettledSample { get; private set; }

        // Returns true exactly once: on the frame the sample settles. advancedAnimationSeconds is how far the rider's animation
        // advanced since the previous frame (0 while paused or not evaluated).
        public bool Observe(PoseVector3 seatFromSource, float advancedAnimationSeconds)
        {
            if (Settled || GaveUp || !seatFromSource.IsFinite) return false;
            if (!(advancedAnimationSeconds > 0f) || float.IsInfinity(advancedAnimationSeconds))
            {
                IgnoredFrames++;
                return false;
            }
            Frames++;
            AnimationSeconds += advancedAnimationSeconds;
            if (!hasWindow || (seatFromSource - windowStart).Magnitude > StableMetres)
            {
                windowStart = seatFromSource;
                windowSeconds = 0f;
                hasWindow = true;
            }
            else
            {
                windowSeconds += advancedAnimationSeconds;
            }
            if (AnimationSeconds >= MinimumAnimationSeconds && windowSeconds >= StableWindowSeconds)
            {
                Settled = true;
                FramesToSettle = Frames;
                AnimationSecondsToSettle = AnimationSeconds;
                SettledSample = seatFromSource;
                return true;
            }
            if (AnimationSeconds >= MaximumAnimationSeconds) GaveUp = true;
            return false;
        }
    }
}
