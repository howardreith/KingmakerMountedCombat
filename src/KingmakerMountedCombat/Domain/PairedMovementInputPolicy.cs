namespace KingmakerMountedCombat.Domain
{
    // F3: which turn context owns a pair's ground preview and travel, and which ground
    // movement limit the mount takes from the rider's native input context. Production
    // (MountedPairTurnParticipant.PairedMovementContext) decides through these rules.
    internal static class PairedMovementInputPolicy
    {
        // Locomotion belongs to the mount whichever pair actor is selected.
        internal static bool MountOwnsLocomotion(bool partnerAddressable, bool riderSelected, bool mountSelected) =>
            partnerAddressable && (riderSelected || mountSelected);

        // Rider-selected input is mirrored onto the mount only while the mount can take it.
        internal static bool MirrorsRiderInput(bool riderSelected, bool mountControllable, bool mountMoving) =>
            riderSelected && mountControllable && !mountMoving;

        // The rider context's limit is a ground choice only while the rider still has normal
        // movement and nothing is hovered: native forces a step limit on a spent rider and a
        // hovered target's smart variant rewrites the limit, and neither describes the mount.
        internal static bool IsGroundLimitChoice(bool riderHasNormalMovement, bool hovering) =>
            riderHasNormalMovement && !hovering;

        // The prediction panel's movement-facing slots (five-foot step, Move, Standard, full round) describe the
        // mount only during a rider-selected ground preview whose states were filled from the mount's own context
        // this prediction. Target, object and ability previews keep every slot on the rider, whose attacks,
        // spells and Swift remain its own.
        internal static bool SlotsDescribeMount(bool riderSelected, bool partnerAddressable, bool overTerrain, bool mountStatesFilled) =>
            riderSelected && partnerAddressable && overTerrain && mountStatesFilled;

        // Native's state computation (TurnController.ComputeCurrentActionStates) treats the Move as gone once MoveAction is
        // above zero (UsedOneMoveAction). Mid-turn a partial first move keeps exactly its remaining first-Move travel:
        // movement stays available while that window has range, and the Move's other uses are lost to it.
        internal static bool PartialMoveRemains(float moveCooldown, bool moveRestricted, bool restrictNormalMovement, float remainingMoveFeet) =>
            moveCooldown > 0f && moveCooldown < 3f && !moveRestricted && !restrictNormalMovement && remainingMoveFeet > 0f;

        // Native's state computation (TurnController.ComputeCurrentActionStates) treats the Standard as gone once
        // MoveAction passes three seconds. Mid-turn a started double move keeps exactly its remaining conversion
        // travel: movement stays available while that window has range, and the Standard's other uses are lost to it.
        internal static bool ConversionTravelRemains(float moveCooldown, bool usedStandardAction, bool restrictNormalMovement, float remainingStandardFeet) =>
            moveCooldown > 3f && !usedStandardAction && !restrictNormalMovement && remainingStandardFeet > 0f;

        // The mount keeps its last ground choice; with none this activation it gets the
        // unrestricted limit, which native then validates against the mount's own budget.
        internal static T MountLimit<T>(T riderLimit, bool groundChoice, ref T mirrored, ref bool hasMirrored, T unchosen)
        {
            if (groundChoice) { mirrored = riderLimit; hasMirrored = true; }
            return hasMirrored ? mirrored : unchosen;
        }
    }
}
