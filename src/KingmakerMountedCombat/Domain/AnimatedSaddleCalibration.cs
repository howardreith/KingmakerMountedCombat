namespace KingmakerMountedCombat.Domain
{
    // MMR-01 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). The animated saddle keeps the rider's pelvis at
    // source + seatFromSource + correction every frame, and seatFromSource is acquired once at attachment. Sampling it
    // from the rider's pelvis in that one frame binds the seat height to whatever animation the rider was playing when
    // Mount Companion fired (cast clip, a walk blending out after the approach, a combat stance) or to a view's first
    // evaluated frame after a load: the owner's "hovering high on first mount, better after a remount". The Horse
    // instead declares one seat basis (the pelvis-to-Chest vector its accepted seat correction is believed tuned against),
    // so first mount, mounted load and remount calibrate identically. An animated-saddle profile without a declaration keeps the observed
    // sample; Small profiles keep their own fixed-from-source path; the Mammoth has no animated saddle.
    public static class AnimatedSaddleCalibration
    {
        // The declared basis: the rider's pelvis minus the Horse Chest (the saddle source), in the Horse mount root's local
        // coordinate frame (metres; X anatomical right, Y up, Z forward), to which HorseAnimatedSeatCorrection is added.
        // Provenance (review R1-F12): only pelvis root-X ~ -0.1892 with Chest X = 0 is a documented observation, from the
        // preview.3 final render (docs/PHASE3H-IMPLEMENTATION.md, commit 5b25b96e, which then tuned the +0.19 X correction).
        // The Y and Z values (Chest 1.5024/0.4674, pelvis 2.3625/0.4849, hence Y 0.8601 and Z 0.0175) first appear as the
        // fixture constants of that commit's test; the run's raw seat-geometry log is not retained, so they are not an
        // independently verified measurement. That this basis matches the idle sample a remount takes (the seat the owner
        // found better) is a hypothesis: the seat line's observedSeatFromSource, logged at every attachment, can check it.
        public static readonly PoseVector3 MediumHumanoidHorseSeatFromSource = new PoseVector3(-0.1892f, 0.8601f, 0.0175f);

        // The posed-pelvis sample AnimatedSaddlePosition.Acquire receives.
        public static PoseVector3 CalibrationPelvis(PoseVector3 source, PoseVector3 observedPosedPelvis, PoseVector3? declaredSeatFromSource) =>
            declaredSeatFromSource.HasValue ? source + declaredSeatFromSource.Value : observedPosedPelvis;
    }
}
