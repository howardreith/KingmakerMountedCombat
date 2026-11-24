namespace KingmakerMountedCombat.Domain
{
    // MMR-02 (docs/missions/mounted-alpha2-melee-pose/MISSION.md), diagnostic only (review R1-F3: an admission cannot
    // establish a legal mount route before native's own validation, so alpha 2 does not change native's decision). Native
    // TurnController.IgnoreClick (2.1.7b, 06000C2F) drops a click unless PathVisualizer.IsCurrentPathForPoint(Unit.Position,
    // approach point, approach radius) holds. This names the case to log: native dropped a rider-context click whose own
    // predicted command is a melee UnitAttack by the rider on the highlighted hostile, for the path check alone.
    public static class PairedAttackClickDropDiagnostic
    {
        public static bool DroppedForRiderPath(bool nativeIgnored, bool riderBoundaryContext, bool riderSelected,
            bool riderCommandsEmpty, bool predictionsCurrent, bool overTerrain, bool abilitySelected,
            bool predictedMeleeAttackOnHighlightedHostile, bool pathCheckRefused) =>
            nativeIgnored && riderBoundaryContext && riderSelected && riderCommandsEmpty && predictionsCurrent && !overTerrain &&
            !abilitySelected && predictedMeleeAttackOnHighlightedHostile && pathCheckRefused;
    }
}
