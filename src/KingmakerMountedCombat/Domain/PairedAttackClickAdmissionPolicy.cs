namespace KingmakerMountedCombat.Domain
{
    // MMR-02 (docs/missions/mounted-alpha2-melee-pose/MISSION.md). Native TurnController.IgnoreClick (2.1.7b, 06000C2F)
    // simulates the click and then drops it unless PathVisualizer.IsCurrentPathForPoint(Unit.Position, approach point,
    // approach radius) holds: a current visualized path must end within the predicted command's approach radius of the
    // target ("Ignoring click: path not current"). With the rider selected in a paired activation, locomotion belongs to
    // the mount (PairedMovementContext), so the visualized path is not a rider path ending within the rider's reach, and an
    // ordinary attack on a hostile just beyond reach can be dropped before any pair intent exists. Review R1-F3: only that
    // exact refusal is overridden. Native's own predicted command must be a melee UnitAttack by the rider on the highlighted
    // hostile, the path check must be the refusal (recomputed), every earlier native refusal (busy commands, stale
    // predictions, terrain, a selected ability) stands, and the mount's lawful remaining movement (step or move allowance
    // from the coordinator's own allocation) must cover the straight-line distance to the pair's reach. A blocked or longer
    // route is still refused later by the pair command's own approach; any other click keeps native's refusal and the
    // prior order.
    public static class PairedAttackClickAdmissionPolicy
    {
        public static bool AdmitsDroppedRiderAttackClick(bool nativeIgnored, bool riderBoundaryContext, bool riderSelected,
            bool riderCommandsEmpty, bool predictionsCurrent, bool overTerrain, bool abilitySelected,
            bool predictedMeleeAttackOnHighlightedHostile, bool pathCheckRefused, bool mountCanMove,
            float mountRemainingMetres, float pairDistanceMetres, float pairReachMetres) =>
            nativeIgnored && riderBoundaryContext && riderSelected && riderCommandsEmpty && predictionsCurrent && !overTerrain &&
            !abilitySelected && predictedMeleeAttackOnHighlightedHostile && pathCheckRefused && mountCanMove &&
            IsFinite(mountRemainingMetres) && IsFinite(pairDistanceMetres) && IsFinite(pairReachMetres) &&
            mountRemainingMetres >= System.Math.Max(0f, pairDistanceMetres - pairReachMetres);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    }
}
