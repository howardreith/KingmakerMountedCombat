namespace KingmakerMountedCombat.Domain
{
    public enum MountedGroundOrderDisposition
    {
        // The order is not this pair's: no fact is read and nothing of this pair is touched.
        PassThroughNotThisPair,
        SuppressPropagatedOverlayClick,
        RefuseSaveSerialization,
        RefuseGameMode,
        AdmitRealTime,
        RefusePairedMovementUnavailable,
        RefuseWrongTurn,
        RefuseMountMoveSlotBusy,
        AdmitRiderTurnMovement
    }

    // The live facts and the one pair-local side effect the ground-order admission consumes.
    // Each member is reached only when the decision needs it, in the order the decision needs
    // it, so a seam observes exactly which facts were read and when the pair's own commands
    // were cancelled.
    public interface IMountedGroundOrderFacts
    {
        bool TryConsumePropagatedOverlayWorldClick();
        bool SaveSerializationSuspended { get; }
        // Cancel("ground command") on this pair only.
        void CancelPairCommands();
        // The exact native game-mode name, or null without a game.
        string CurrentModeName { get; }
        bool InTurnBasedCombat { get; }
        bool PairedActivationEnabled { get; }
        bool CanMovePairedMount();
        bool CurrentTurnUnitIsRider { get; }
        bool RiderTurnPreparing { get; }
        bool RiderTurnActing { get; }
        bool MountMoveSlotIdle { get; }
    }

    // The production paused/real-time/turn-based ground-order admission for one mounted pair.
    // MountedCombatController.TryAdmitGroundCommand delegates to Decide and applies only the
    // disposition's feedback and rejection codes (and, for an admitted rider-turn move, its
    // own pair-owner bookkeeping).
    public static class MountedGroundOrderAdmissionPolicy
    {
        public static bool IsThisPairOrder(bool disposed, bool relationshipMounted,
            bool requestedUnitIsRider, bool pairedMountInput) =>
            !disposed && relationshipMounted && (requestedUnitIsRider || pairedMountInput);

        public static MountedGroundOrderDisposition Decide(bool disposed, bool relationshipMounted,
            bool requestedUnitIsRider, bool pairedMountInput, IMountedGroundOrderFacts facts)
        {
            if (!IsThisPairOrder(disposed, relationshipMounted, requestedUnitIsRider, pairedMountInput))
            {
                return MountedGroundOrderDisposition.PassThroughNotThisPair;
            }
            if (facts.TryConsumePropagatedOverlayWorldClick())
            {
                return MountedGroundOrderDisposition.SuppressPropagatedOverlayClick;
            }

            // The archive worker serializes live object graphs, including this actor's command
            // queue, and entity execution stays suspended for its whole lifetime. Accepting a new
            // owned command - or cancelling the current one - would still mutate that queue under
            // the serializer, so the owned pair refuses before any cancel until the save settles. The
            // flag is held across both the ordinary completion path and an interrupted save that is
            // still draining.
            if (facts.SaveSerializationSuspended)
            {
                return MountedGroundOrderDisposition.RefuseSaveSerialization;
            }

            facts.CancelPairCommands();

            var mode = facts.CurrentModeName;
            // A real-time ground order given while paused queues natively, as it does for an
            // unmounted unit; turn-based admission still requires the active world view.
            if (mode == null || !(MountedGameModePolicy.CanAdmitMountedAction(mode) ||
                MountedGameModePolicy.CanQueueMountedAction(mode) && !facts.InTurnBasedCombat))
            {
                return MountedGroundOrderDisposition.RefuseGameMode;
            }

            if (!facts.InTurnBasedCombat)
            {
                return MountedGroundOrderDisposition.AdmitRealTime;
            }

            if (facts.PairedActivationEnabled && !facts.CanMovePairedMount())
            {
                return MountedGroundOrderDisposition.RefusePairedMovementUnavailable;
            }
            if (!MountedPairTurnPolicy.CanAdmitRiderGroundMovement(
                true,
                true,
                true,
                facts.CurrentTurnUnitIsRider,
                facts.RiderTurnPreparing,
                facts.RiderTurnActing))
            {
                return MountedGroundOrderDisposition.RefuseWrongTurn;
            }
            if (!facts.MountMoveSlotIdle)
            {
                return MountedGroundOrderDisposition.RefuseMountMoveSlotBusy;
            }
            return MountedGroundOrderDisposition.AdmitRiderTurnMovement;
        }

        // The native ClickGroundHandler.RunCommand prefix result for the disposition.
        public static bool LetsNativeOrderRun(MountedGroundOrderDisposition disposition) =>
            disposition == MountedGroundOrderDisposition.PassThroughNotThisPair ||
            disposition == MountedGroundOrderDisposition.AdmitRealTime ||
            disposition == MountedGroundOrderDisposition.AdmitRiderTurnMovement;

        // Null leaves the controller's previous rejection codes untouched.
        public static MountedCombatRejectionCode[] RejectionCodes(MountedGroundOrderDisposition disposition)
        {
            switch (disposition)
            {
                case MountedGroundOrderDisposition.RefuseSaveSerialization:
                case MountedGroundOrderDisposition.RefuseGameMode:
                case MountedGroundOrderDisposition.RefusePairedMovementUnavailable:
                    return new[] { MountedCombatRejectionCode.WrongActionState };
                case MountedGroundOrderDisposition.RefuseWrongTurn:
                    return new[] { MountedCombatRejectionCode.WrongTurn };
                case MountedGroundOrderDisposition.RefuseMountMoveSlotBusy:
                    return new[] { MountedCombatRejectionCode.AlreadyActiveCommand };
                case MountedGroundOrderDisposition.AdmitRealTime:
                case MountedGroundOrderDisposition.AdmitRiderTurnMovement:
                    return new MountedCombatRejectionCode[0];
                default:
                    return null;
            }
        }

        // Null leaves the controller's previous feedback untouched.
        public static string Feedback(MountedGroundOrderDisposition disposition, string mountDisplayName)
        {
            switch (disposition)
            {
                case MountedGroundOrderDisposition.RefuseSaveSerialization:
                    return "A save is still being written; mounted actions resume when it finishes.";
                case MountedGroundOrderDisposition.RefuseGameMode:
                    return "Mounted ground movement is available only in the active world view.";
                case MountedGroundOrderDisposition.RefusePairedMovementUnavailable:
                    return "The mount has no movement available in this paired activation.";
                case MountedGroundOrderDisposition.RefuseWrongTurn:
                    return "Move the mounted pair during the rider's turn.";
                case MountedGroundOrderDisposition.RefuseMountMoveSlotBusy:
                    return "Mounted ground movement rejected: the " + mountDisplayName + " Move slot is not idle.";
                case MountedGroundOrderDisposition.AdmitRiderTurnMovement:
                    return "Mounted rider-turn ground movement admitted; " + mountDisplayName + " owns pathfinding.";
                default:
                    return null;
            }
        }
    }
}
