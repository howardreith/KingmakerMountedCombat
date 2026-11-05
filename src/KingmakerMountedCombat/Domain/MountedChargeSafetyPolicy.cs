namespace KingmakerMountedCombat.Domain
{
    // F2 CTRL-01: which Charge the ordinary stock Charge is for this actor.
    public enum MountedChargeRoute
    {
        // Unmounted, unrelated, or not the exact stock Charge: untouched native behavior.
        Native,
        // The mounted rider: availability, reason and targets come from the mounted Charge, and the click selects its shell.
        Mounted,
        // The carried mount: the rider orders the pair's Charge.
        Refused
    }

    public static class MountedChargeSafetyPolicy
    {
        public const string ChargeBlueprintId = "c78506dd0e14f7c45a599990e4e65038";
        // A stock Charge command reached admission for a mounted pair: it was queued before the rider mounted, or came
        // from automatic use. It is cancelled, never converted into the mounted Charge.
        public const string Feedback = "Charge was cancelled because the rider's mount state changed. Choose Charge again.";
        public const string MountFeedback = "While carrying a rider, the mount charges when the rider uses Charge.";

        public static bool ShouldReject(RelationshipState state, bool belongsToPair,
            string blueprintId, bool exactNativeChargeLogic, bool alreadyActed = false)
        {
            return state == RelationshipState.Mounted && belongsToPair &&
                blueprintId == ChargeBlueprintId && exactNativeChargeLogic && !alreadyActed;
        }

        public static MountedChargeRoute Route(RelationshipState state, bool belongsToPair, bool unitIsRider,
            string blueprintId, bool exactNativeChargeLogic)
        {
            if (!ShouldReject(state, belongsToPair, blueprintId, exactNativeChargeLogic)) return MountedChargeRoute.Native;
            return unitIsRider ? MountedChargeRoute.Mounted : MountedChargeRoute.Refused;
        }
    }
}
