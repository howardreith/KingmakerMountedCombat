using System;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedGameModeDisposition
    {
        PreserveWorldInteraction,
        PreserveNonWorldUi,
        CleanDismount
    }

    public static class MountedGameModePolicy
    {
        public static MountedGameModeDisposition Classify(string exactModeName)
        {
            if (string.Equals(exactModeName, "Default", StringComparison.Ordinal))
            {
                return MountedGameModeDisposition.PreserveWorldInteraction;
            }

            if (string.Equals(exactModeName, "Pause", StringComparison.Ordinal) ||
                string.Equals(exactModeName, "FullScreenUi", StringComparison.Ordinal) ||
                string.Equals(exactModeName, "EscMode", StringComparison.Ordinal))
            {
                return MountedGameModeDisposition.PreserveNonWorldUi;
            }

            return MountedGameModeDisposition.CleanDismount;
        }

        public static bool CanRetainMountedRelationship(string exactModeName)
        {
            return Classify(exactModeName) != MountedGameModeDisposition.CleanDismount;
        }

        public static bool CanAdmitMountedAction(string exactModeName)
        {
            return Classify(exactModeName) == MountedGameModeDisposition.PreserveWorldInteraction;
        }
    }

    public enum MountedViewAttachmentDisposition
    {
        IgnoreNonPair,
        ObserveExactView,
        CleanReplacementFromOwnedAnchor,
        CleanChangedView
    }

    public static class MountedViewAttachmentPolicy
    {
        public static MountedViewAttachmentDisposition Classify(
            bool relationshipMounted,
            bool attachedUnitIsExactPair,
            bool attachedViewIsCapturedView,
            bool changedViewIsChildOfOwnedAnchor)
        {
            if (!relationshipMounted || !attachedUnitIsExactPair)
            {
                return MountedViewAttachmentDisposition.IgnoreNonPair;
            }
            if (attachedViewIsCapturedView)
            {
                return MountedViewAttachmentDisposition.ObserveExactView;
            }
            return changedViewIsChildOfOwnedAnchor
                ? MountedViewAttachmentDisposition.CleanReplacementFromOwnedAnchor
                : MountedViewAttachmentDisposition.CleanChangedView;
        }
    }

    public static class MountedViewActivityPolicy
    {
        public static bool IsAdmissible(
            MountedGameModeDisposition gameMode,
            bool riderActiveSelf,
            bool mountActiveSelf,
            bool riderActiveInHierarchy,
            bool mountActiveInHierarchy)
        {
            if (gameMode == MountedGameModeDisposition.PreserveNonWorldUi)
            {
                // Full-screen UI may temporarily hide either world view through stock
                // visibility state. Exact view identity and detach/replacement events
                // remain the relationship authority while the world is covered.
                return true;
            }

            return gameMode == MountedGameModeDisposition.PreserveWorldInteraction &&
                riderActiveSelf && mountActiveSelf &&
                riderActiveInHierarchy && mountActiveInHierarchy;
        }
    }

    public enum NativeTurnBasedExitAiLeaseDisposition
    {
        NotPending,
        AwaitNativeControllerClear,
        RejectInexactLease,
        AlreadyExact,
        ReassertExactLease
    }

    public static class NativeTurnBasedExitAiLeasePolicy
    {
        public static NativeTurnBasedExitAiLeaseDisposition Classify(
            bool exitDeliveryPending,
            bool relationshipMounted,
            bool nativeTurnBasedPredicate,
            bool nativeControllerInitialized,
            bool mountAiLeaseOwned,
            bool mountRawAiEnabled)
        {
            if (!exitDeliveryPending)
            {
                return NativeTurnBasedExitAiLeaseDisposition.NotPending;
            }

            if (nativeTurnBasedPredicate || nativeControllerInitialized)
            {
                return NativeTurnBasedExitAiLeaseDisposition.AwaitNativeControllerClear;
            }

            if (!relationshipMounted || !mountAiLeaseOwned)
            {
                return NativeTurnBasedExitAiLeaseDisposition.RejectInexactLease;
            }

            return mountRawAiEnabled
                ? NativeTurnBasedExitAiLeaseDisposition.ReassertExactLease
                : NativeTurnBasedExitAiLeaseDisposition.AlreadyExact;
        }
    }

    public static class MountedStockAttackPolicy
    {
        public static bool ShouldReject(
            bool relationshipMounted,
            bool ownerIsExactRider,
            bool commandIsExactStockUnitAttack)
        {
            return relationshipMounted && ownerIsExactRider && commandIsExactStockUnitAttack;
        }

        public static string RejectionFeedback(bool selectedWeaponIsRanged)
        {
            return selectedWeaponIsRanged
                ? "Mounted ranged attacks are not supported in this private alpha."
                : "Use Rider melee, then click one visible hostile target.";
        }
    }

    public sealed class MountedOverlayWorldInputGuard
    {
        private const int MaximumPropagationFrameDelta = 2;
        private int activationFrame;
        private bool pending;

        public void MarkActivation(int frame)
        {
            if (frame < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frame));
            }

            activationFrame = frame;
            pending = true;
        }

        public bool TryConsumePropagatedWorldClick(int frame)
        {
            if (!pending)
            {
                return false;
            }

            pending = false;
            var delta = frame - activationFrame;
            return delta >= 0 && delta <= MaximumPropagationFrameDelta;
        }

        public void Clear()
        {
            pending = false;
        }
    }

    public static class MountedCleanupFeedbackPolicy
    {
        public static string Describe(CleanupTrigger trigger)
        {
            switch (trigger)
            {
                case CleanupTrigger.SaveRequested:
                    return "Dismounted cleanly because saving is a transient-mounted-state boundary.";
                case CleanupTrigger.LoadRequested:
                    return "Dismounted cleanly because loading replaces the current world state.";
                case CleanupTrigger.AreaUnloading:
                    return "Dismounted cleanly because the current area is unloading or changing.";
                case CleanupTrigger.ViewReplaced:
                    return "Dismounted cleanly because the rider body or world view was replaced.";
                case CleanupTrigger.ViewDetached:
                    return "Dismounted cleanly because the rider or Mammoth world view detached.";
                case CleanupTrigger.Death:
                    return "Dismounted cleanly because a mounted pair member died.";
                case CleanupTrigger.Incapacitated:
                    return "Dismounted cleanly because a mounted pair member became unconscious.";
                case CleanupTrigger.CompanionInvalidated:
                    return "Dismounted cleanly because the rider/Mammoth relationship became invalid.";
                case CleanupTrigger.GameModeBoundary:
                    return "Dismounted cleanly because the game left an active world or non-world menu mode.";
                case CleanupTrigger.ModDisabled:
                    return "Dismounted cleanly because Kingmaker Mounted Combat was disabled.";
                case CleanupTrigger.Exception:
                    return "Dismounted during fail-closed mounted-state recovery.";
                default:
                    return "Dismounted cleanly: " + trigger + ".";
            }
        }
    }
}
