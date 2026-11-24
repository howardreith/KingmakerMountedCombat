namespace KingmakerMountedCombat.Domain
{
    // MMR-02 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). Native TurnController.IgnoreClick (2.1.7b, 06000C2F)
    // simulates the click and then drops it unless PathVisualizer.IsCurrentPathForPoint(Unit.Position, approach point,
    // approach radius) holds: a current visualized path must end within the predicted command's approach radius of the
    // target ("Ignoring click: path not current"). With the rider selected in a paired activation, locomotion belongs to
    // the mount (PairedMovementContext), so the visualized path is not a rider path ending within the rider's reach, and an
    // ordinary attack on a hostile just beyond reach is dropped before any pair intent exists. A target already in reach
    // needs no path: the owner's "when I move closer, it attacks". The pair's own attack command approaches with the
    // mount's lawful remaining movement and refuses explainably when it cannot reach, so only that dropped click is admitted.
    public static class PairedAttackClickAdmissionPolicy
    {
        public static bool AdmitsDroppedRiderAttackClick(bool nativeIgnored, bool riderBoundaryContext, bool riderSelected,
            bool riderCommandsEmpty, bool predictionsCurrent, bool overTerrain, bool abilitySelected, bool hostileUnitHighlighted,
            bool mountHasLawfulMovement) =>
            nativeIgnored && riderBoundaryContext && riderSelected && riderCommandsEmpty && predictionsCurrent && !overTerrain &&
            !abilitySelected && hostileUnitHighlighted && mountHasLawfulMovement;
    }
}
