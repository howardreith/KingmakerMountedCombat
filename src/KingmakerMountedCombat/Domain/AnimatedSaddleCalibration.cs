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
    // Settling (reviews R5-F4, round 6) counts only frames in which the rider's animation advanced (its animation manager
    // enabled and not disabled, and a positive game delta: Kingmaker's pause suspends Time.timeScale). No installed contract
    // guarantees that an enabled manager actually EVALUATED the rider pose in a counted frame (the native AnimationManager's
    // PlayableGraph runs in DirectorUpdateMode.GameTime, subject to Animator culling), so the sampler also requires evidence
    // of evaluation: a window settles only if the sampled rider-local pose changed frame to frame by more than
    // EvaluationEpsilonMetres in at least MinimumEvidenceFrames of its frames (a living idle breathes; a bit-frozen pose never
    // settles). A window settles once the sample stays within StableMetres of the window's FIRST sample for
    // StableWindowSeconds of animation time, after at least MinimumAnimationSeconds, and applies the window's mean. After a
    // settle the basis stays correctable for CorrectionHorizonSeconds of advancing animation (at most MaximumCorrections): a
    // later evidenced stable window whose mean differs from the applied basis by more than CorrectionThresholdMetres is applied
    // and logged as phase=corrected, so a pose that was withheld or held when the first window settled is still replaced by
    // the rider's real resting pose.
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

    public enum SeatSampleOutcome
    {
        None,
        Settled,
        Corrected,
        GaveUp
    }

    // The settle and correction decision for one attachment, in animation time with evaluation evidence (reviews R5-F4, round 6).
    public sealed class SettledSeatSampler
    {
        // Longer than the Mount Companion cast and an approach walk's blend, measured only while animation advances.
        public const float MinimumAnimationSeconds = 2f;
        public const float StableWindowSeconds = 0.5f;
        public const float StableMetres = 0.002f;
        // Evaluation evidence: a frame-to-frame change of the sampled pose above float noise (0.01 mm).
        public const float EvaluationEpsilonMetres = 0.00001f;
        public const int MinimumEvidenceFrames = 3;
        public const float MaximumAnimationSeconds = 30f;
        public const float CorrectionHorizonSeconds = 30f;
        public const float CorrectionThresholdMetres = 0.01f;
        public const int MaximumCorrections = 2;

        private PoseVector3 windowStart, windowSum, previous;
        private bool hasWindow, hasPrevious;
        private float windowSeconds, settledAtSeconds;
        private int windowCount, windowEvidence;

        public int Frames { get; private set; }
        public int IgnoredFrames { get; private set; }
        public float AnimationSeconds { get; private set; }
        public bool Settled { get; private set; }
        public bool GaveUp { get; private set; }
        public int Corrections { get; private set; }
        public int FramesToSettle { get; private set; }
        public float AnimationSecondsToSettle { get; private set; }
        // The basis currently applied by the sampler: the first settled window's mean, then each correction's.
        public PoseVector3 SettledSample { get; private set; }
        public int LastWindowEvidenceFrames => windowEvidence;

        // Still sampling: not given up, and (before the first settle, or inside the correction horizon with corrections left).
        public bool Observing => !GaveUp && (!Settled || Corrections < MaximumCorrections && AnimationSeconds - settledAtSeconds < CorrectionHorizonSeconds);

        // advancedAnimationSeconds is how far the rider's animation advanced since the previous frame (0 while paused, disabled
        // or not evaluated by the native clock). Returns Settled once, Corrected for each applied correction, GaveUp once.
        public SeatSampleOutcome Observe(PoseVector3 seatFromSource, float advancedAnimationSeconds)
        {
            if (!Observing || !seatFromSource.IsFinite) return SeatSampleOutcome.None;
            if (!(advancedAnimationSeconds > 0f) || float.IsInfinity(advancedAnimationSeconds))
            {
                IgnoredFrames++;
                return SeatSampleOutcome.None;
            }
            Frames++;
            AnimationSeconds += advancedAnimationSeconds;
            var evidenced = hasPrevious && (seatFromSource - previous).Magnitude > EvaluationEpsilonMetres;
            previous = seatFromSource;
            hasPrevious = true;
            if (!hasWindow || (seatFromSource - windowStart).Magnitude > StableMetres)
            {
                StartWindow(seatFromSource);
            }
            else
            {
                windowSeconds += advancedAnimationSeconds;
                windowSum = windowSum + seatFromSource;
                windowCount++;
                if (evidenced) windowEvidence++;
            }
            if (AnimationSeconds >= MinimumAnimationSeconds && windowSeconds >= StableWindowSeconds && windowEvidence >= MinimumEvidenceFrames)
            {
                var mean = windowSum * (1f / windowCount);
                if (!Settled)
                {
                    Settled = true;
                    SettledSample = mean;
                    settledAtSeconds = AnimationSeconds;
                    FramesToSettle = Frames;
                    AnimationSecondsToSettle = AnimationSeconds;
                    StartWindow(seatFromSource);
                    return SeatSampleOutcome.Settled;
                }
                if ((mean - SettledSample).Magnitude > CorrectionThresholdMetres)
                {
                    Corrections++;
                    SettledSample = mean;
                    StartWindow(seatFromSource);
                    return SeatSampleOutcome.Corrected;
                }
            }
            if (!Settled && AnimationSeconds >= MaximumAnimationSeconds)
            {
                GaveUp = true;
                return SeatSampleOutcome.GaveUp;
            }
            return SeatSampleOutcome.None;
        }

        private void StartWindow(PoseVector3 first)
        {
            windowStart = first;
            windowSum = first;
            windowCount = 1;
            windowSeconds = 0f;
            windowEvidence = 0;
            hasWindow = true;
        }
    }
}
