using System.Globalization;

namespace KingmakerMountedCombat.Domain
{
    // MMR-01 (docs/missions/mounted-alpha2-melee-pose/MISSION.md; review R1-F12). The animated saddle keeps the rider's pelvis
    // at source + seatFromSource + correction every frame, where source is the Horse Chest and every vector is in the Horse
    // mount root's local frame (metres; X anatomical right, Y up, Z forward). Sampling seatFromSource once, in the frame Mount
    // Companion fired, binds the seat height to whatever the rider was playing then (the cast clip, an approach walk blending
    // out) or to a view's first evaluated frame after a load: the owner's "hovering high on first mount, better after a
    // remount" (a remount is taken from an idle stance).
    //
    // The Horse basis is therefore the rider's OWN settled pose, measured live: the saddle is acquired provisionally from the
    // attachment-frame sample (the pre-alpha-2 behavior), then the adapter observes the rider's animated pelvis-minus-Chest
    // vector in the mount root frame every frame (after native animation and the profile pelvis offset, before KMC's seat
    // write) and re-acquires the saddle once from the first sample that holds still (SettledSeatSampler). First mount, mounted
    // load and remount all settle on the same rider-rig idle sample; no literal basis is involved. HorseAnimatedSeatCorrection
    // (the accepted +0.19/-0.16/-0.18 visual correction) is still added relative to it. Each attachment logs the attachment
    // sample and the settled sample with the values applied (FormatNotice), so the owner's log shows both.
    // Mammoth has no animated saddle; Small profiles keep their fixed-from-source seat.
    public static class AnimatedSaddleCalibration
    {
        public static string FormatNotice(string phase, string riderId, PoseVector3 observedSeatFromSource, PoseVector3 appliedSeatFromSource,
            PoseVector3 seatCorrection, int frames) =>
            "Horse seatCalibration: phase=" + phase + "; rider=" + (riderId ?? "<none>") +
            "; observedSeatFromSource=" + Format(observedSeatFromSource) + "; appliedSeatFromSource=" + Format(appliedSeatFromSource) +
            "; seatCorrection=" + Format(seatCorrection) + "; frames=" + frames + ".";

        public static string Format(PoseVector3 value) =>
            "(" + value.X.ToString("F4", CultureInfo.InvariantCulture) + "," + value.Y.ToString("F4", CultureInfo.InvariantCulture) + "," +
            value.Z.ToString("F4", CultureInfo.InvariantCulture) + ")";
    }

    // The settle decision for one attachment. A sample settles once at least MinimumFrames have passed (so the Mount Companion
    // cast clip or an approach walk has ended) and it has stayed within StableMetres of the previous frame's sample for
    // StableFrames consecutive frames. A pose that never holds still within MaximumFrames keeps the provisional basis.
    public sealed class SettledSeatSampler
    {
        // About two seconds at 60 fps: longer than the Mount Companion cast and the approach walk's blend.
        public const int MinimumFrames = 120;
        public const int StableFrames = 20;
        public const float StableMetres = 0.002f;
        public const int MaximumFrames = 1800;

        private PoseVector3 last;
        private bool hasLast;
        private int stableRun;

        public int Frames { get; private set; }
        public bool Settled { get; private set; }
        public bool GaveUp { get; private set; }
        public int FramesToSettle { get; private set; }
        public PoseVector3 SettledSample { get; private set; }

        // Returns true exactly once: on the frame the sample settles.
        public bool Observe(PoseVector3 seatFromSource)
        {
            if (Settled || GaveUp || !seatFromSource.IsFinite) return false;
            Frames++;
            stableRun = hasLast && (seatFromSource - last).Magnitude < StableMetres ? stableRun + 1 : 0;
            last = seatFromSource;
            hasLast = true;
            if (Frames >= MinimumFrames && stableRun >= StableFrames)
            {
                Settled = true;
                FramesToSettle = Frames;
                SettledSample = seatFromSource;
                return true;
            }
            if (Frames >= MaximumFrames) GaveUp = true;
            return false;
        }
    }
}
