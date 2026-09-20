namespace KingmakerMountedCombat.Domain
{
    public static class MountedChargeSafetyPolicy
    {
        public const string ChargeBlueprintId = "c78506dd0e14f7c45a599990e4e65038";
        public const string Feedback = "Charge is not yet supported while mounted.";

        public static bool ShouldReject(RelationshipState state, bool belongsToPair,
            string blueprintId, bool exactNativeChargeLogic, bool alreadyActed = false)
        {
            return state == RelationshipState.Mounted && belongsToPair &&
                blueprintId == ChargeBlueprintId && exactNativeChargeLogic && !alreadyActed;
        }
    }
}
