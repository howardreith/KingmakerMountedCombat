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
    // The reference is confirmed from the rider's RESTING pose, from three separate facts per frame (reviews R5-F4, R7-F2, R7-F3):
    //  1. fresh native evaluation with advancing animation time (the adapter reads it from the installed contracts: an enabled
    //     native AnimationManager whose PlayableGraph is valid and playing, its Animator enabled with Kingmaker's AlwaysAnimate
    //     culling mode, not Disabled, and a positive game delta);
    //  2. eligibility of the current pose as a resting reference (the native idle stage: UnitAnimationManager.CanRunIdleAction,
    //     and no released action still blending out);
    //  3. stability of the eligible samples (within StableMetres of the window's FIRST sample for StableWindowSeconds).
    // A window settles only on consecutive evaluated, eligible, stable frames, after MinimumAnimationSeconds of evaluated time
    // since attachment, and confirms the window's mean once. A perfectly stationary idle is a positive case: no motion is
    // required. Unevaluated or non-advancing frames (disabled, paused, graph stopped) and ineligible frames (moving, attacking,
    // casting, micro-idles, transitions) consume nothing and end the current window, so when evaluation and rest eligibility
    // return a fresh window begins. Nothing exhausts the calibration: a pose that has not settled after UnsettledNoticeSeconds of
    // evaluated time is logged once as phase=unsettled and the sampler keeps observing. A confirmed reference is never
    // overwritten (later movement, attacks, casts and transitions cannot replace it).
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

    public enum SeatSampleOutcome
    {
        None,
        Settled,
        // Logged once when no resting window settled within UnsettledNoticeSeconds of evaluated time; observation continues.
        Unsettled
    }

    // The settle decision for one attachment (reviews R5-F4, R7-F2, R7-F3).
    public sealed class SettledSeatSampler
    {
        // Longer than the Mount Companion cast and an approach walk's blend, measured only while animation is evaluated and advances.
        public const float MinimumAnimationSeconds = 2f;
        public const float StableWindowSeconds = 0.5f;
        public const float StableMetres = 0.002f;
        public const float UnsettledNoticeSeconds = 30f;

        private PoseVector3 windowStart, windowSum;
        private bool hasWindow;
        private float windowSeconds;
        private int windowCount;

        public int Frames { get; private set; }
        // Frames without fresh evaluation or advancing time (disabled, paused, graph stopped): never counted.
        public int IgnoredFrames { get; private set; }
        // Evaluated frames whose pose was not a resting reference (moving, acting, casting, idle variant, transition).
        public int IneligibleFrames { get; private set; }
        public float AnimationSeconds { get; private set; }
        public bool Settled { get; private set; }
        public bool UnsettledReported { get; private set; }
        public int FramesToSettle { get; private set; }
        public float AnimationSecondsToSettle { get; private set; }
        // The confirmed resting reference (valid once Settled).
        public PoseVector3 SettledSample { get; private set; }

        public bool Observing => !Settled;

        // evaluatedAdvanceSeconds: how far the rider's freshly evaluated animation advanced since the previous frame (0 when not
        // evaluated or not advancing). restEligible: whether this frame's pose is a resting reference.
        public SeatSampleOutcome Observe(PoseVector3 seatFromSource, float evaluatedAdvanceSeconds, bool restEligible)
        {
            if (!Observing) return SeatSampleOutcome.None;
            // PR-3 review R1-F3: an invalid sample breaks continuity; the next finite, evaluated, eligible sample starts a fresh window.
            if (!seatFromSource.IsFinite) { hasWindow = false; return SeatSampleOutcome.None; }
            if (!(evaluatedAdvanceSeconds > 0f) || float.IsInfinity(evaluatedAdvanceSeconds))
            {
                IgnoredFrames++;
                hasWindow = false;
                return SeatSampleOutcome.None;
            }
            Frames++;
            AnimationSeconds += evaluatedAdvanceSeconds;
            if (!restEligible)
            {
                IneligibleFrames++;
                hasWindow = false;
            }
            else
            {
                if (!hasWindow || (seatFromSource - windowStart).Magnitude > StableMetres)
                {
                    windowStart = seatFromSource;
                    windowSum = seatFromSource;
                    windowCount = 1;
                    windowSeconds = 0f;
                    hasWindow = true;
                }
                else
                {
                    windowSeconds += evaluatedAdvanceSeconds;
                    windowSum = windowSum + seatFromSource;
                    windowCount++;
                }
                if (AnimationSeconds >= MinimumAnimationSeconds && windowSeconds >= StableWindowSeconds)
                {
                    Settled = true;
                    SettledSample = windowSum * (1f / windowCount);
                    FramesToSettle = Frames;
                    AnimationSecondsToSettle = AnimationSeconds;
                    return SeatSampleOutcome.Settled;
                }
            }
            if (!UnsettledReported && AnimationSeconds >= UnsettledNoticeSeconds)
            {
                UnsettledReported = true;
                return SeatSampleOutcome.Unsettled;
            }
            return SeatSampleOutcome.None;
        }
    }
}
