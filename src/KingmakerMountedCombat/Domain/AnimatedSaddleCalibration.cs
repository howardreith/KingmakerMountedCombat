namespace KingmakerMountedCombat.Domain
{
    // MMR-01 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). The animated saddle keeps the rider's pelvis at
    // source + seatFromSource + correction every frame, and seatFromSource is acquired once at attachment. Sampling it
    // from the rider's pelvis in that one frame binds the seat height to whatever animation the rider was playing when
    // Mount Companion fired (cast clip, a walk blending out after the approach, a combat stance) or to a view's first
    // evaluated frame after a load: the owner's "hovering high on first mount, better after a remount". The Horse
    // instead declares the measured seat (the pelvis-to-Chest vector its accepted seat correction was tuned against),
    // so first mount, mounted load and remount calibrate identically. A profile without a declaration keeps the observed
    // sample (Mammoth); Small profiles keep their own fixed-from-source path.
    public static class AnimatedSaddleCalibration
    {
        // Mount-root-local pelvis minus Chest from the measured preview.3 final render (docs/PHASE3H-IMPLEMENTATION.md:
        // Chest (0, 1.5024, 0.4674), pelvis (-0.1892, 2.3625, 0.4849)); HorseAnimatedSeatCorrection is relative to it.
        public static readonly PoseVector3 MediumHumanoidHorseSeatFromSource = new PoseVector3(-0.1892f, 0.8601f, 0.0175f);

        // The posed-pelvis sample AnimatedSaddlePosition.Acquire receives.
        public static PoseVector3 CalibrationPelvis(PoseVector3 source, PoseVector3 observedPosedPelvis, PoseVector3? declaredSeatFromSource) =>
            declaredSeatFromSource.HasValue ? source + declaredSeatFromSource.Value : observedPosedPelvis;
    }
}
