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

        public static bool CanQueueMountedAction(string exactModeName)
        {
            // Queue admission only. Loading, cutscene and actor validity are separate required gates.
            return CanAdmitMountedAction(exactModeName) || string.Equals(exactModeName, "Pause", StringComparison.Ordinal);
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
            bool mountActiveInHierarchy,
            bool nativePartySelectorActive = false)
        {
            if (gameMode == MountedGameModeDisposition.PreserveNonWorldUi ||
                (gameMode == MountedGameModeDisposition.PreserveWorldInteraction && nativePartySelectorActive))
            {
                // Stock UI can temporarily hide a captured world view. The native
                // party selector is also an overlay in Default mode. Exact view
                // identity and the remaining mounted invariants still apply.
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
        public static bool OwnsCompletedCombatEnd(bool exactController, bool controllerCleared,
            bool sameCapturedMount, bool relationshipMounted, bool mountAiLeaseOwned)
        {
            return exactController && controllerCleared && sameCapturedMount && relationshipMounted && mountAiLeaseOwned;
        }

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

    public enum NativeTurnBasedExitUiLeaseDisposition
    {
        NotPending,
        AwaitNativeRealtimeBoundary,
        RejectInexactPair,
        AlreadyExact,
        RestoreExactRiderSelection
    }

    public static class NativeTurnBasedExitUiLeasePolicy
    {
        public static NativeTurnBasedExitUiLeaseDisposition Classify(
            bool exitDeliveryPending,
            bool relationshipMounted,
            bool nativeTurnBasedPredicate,
            string exactGameMode,
            bool aiLeaseBoundaryCompleted,
            bool exactCapturedRiderView,
            bool exactRiderSelected)
        {
            if (!exitDeliveryPending)
            {
                return NativeTurnBasedExitUiLeaseDisposition.NotPending;
            }

            if (nativeTurnBasedPredicate ||
                !string.Equals(exactGameMode, "Default", StringComparison.Ordinal) ||
                !aiLeaseBoundaryCompleted)
            {
                return NativeTurnBasedExitUiLeaseDisposition.AwaitNativeRealtimeBoundary;
            }

            if (!relationshipMounted || !exactCapturedRiderView)
            {
                return NativeTurnBasedExitUiLeaseDisposition.RejectInexactPair;
            }

            return exactRiderSelected
                ? NativeTurnBasedExitUiLeaseDisposition.AlreadyExact
                : NativeTurnBasedExitUiLeaseDisposition.RestoreExactRiderSelection;
        }
    }

    public static class MountedStockAttackPolicy
    {
        public static bool ShouldReject(
            bool relationshipMounted,
            bool ownerIsExactRider,
            bool ownerIsExactMount,
            bool commandIsExactStockUnitAttack)
        {
            return relationshipMounted && (ownerIsExactRider || ownerIsExactMount) &&
                commandIsExactStockUnitAttack;
        }

        public static string RejectionFeedback(bool ownerIsExactMount, bool selectedWeaponIsRanged)
        {
            return RejectionFeedback(ownerIsExactMount, selectedWeaponIsRanged, "Mammoth");
        }

        public static string RejectionFeedback(
            bool ownerIsExactMount,
            bool selectedWeaponIsRanged,
            string mountDisplayName)
        {
            if (ownerIsExactMount)
            {
                return "Use " + (string.IsNullOrWhiteSpace(mountDisplayName) ? "Mount" : mountDisplayName) +
                    " primary, then click one visible hostile target.";
            }
            return selectedWeaponIsRanged
                ? "Mounted ranged attacks are not supported in this private alpha."
                : "Use Rider melee, then click one visible hostile target.";
        }
    }

    public enum MountedSelectionDisposition
    {
        Unchanged,
        ProjectMountToRider,
        PreserveNativeMountTurn
    }

    public static class MountedTurnSelectionPolicy
    {
        public static MountedSelectionDisposition Classify(
            bool relationshipMounted,
            bool requestedUnitIsExactMount,
            bool turnBasedCombat,
            bool currentTurnIsExactMount)
        {
            return Classify(
                relationshipMounted,
                requestedUnitIsExactMount,
                turnBasedCombat,
                currentTurnIsExactMount,
                false);
        }

        public static MountedSelectionDisposition Classify(
            bool relationshipMounted,
            bool requestedUnitIsExactMount,
            bool turnBasedCombat,
            bool currentTurnIsExactMount,
            bool unifiedMountedTurn)
        {
            if (!relationshipMounted || !requestedUnitIsExactMount)
            {
                return MountedSelectionDisposition.Unchanged;
            }

            return turnBasedCombat && currentTurnIsExactMount && !unifiedMountedTurn
                ? MountedSelectionDisposition.PreserveNativeMountTurn
                : MountedSelectionDisposition.ProjectMountToRider;
        }

        public static bool CanUseNativeMountTurnGroundCommand(
            bool relationshipMounted,
            bool turnBasedCombat,
            bool currentTurnIsExactMount,
            bool requestedUnitIsExactMount,
            bool exactMountSelection)
        {
            return relationshipMounted && turnBasedCombat && currentTurnIsExactMount &&
                requestedUnitIsExactMount && exactMountSelection;
        }

        public static bool IsExpectedActionSelection(
            bool turnBasedCombat,
            bool actionIsMountOwned,
            bool exactRiderSelected,
            bool exactMountSelected)
        {
            return IsExpectedActionSelection(
                turnBasedCombat,
                actionIsMountOwned,
                false,
                exactRiderSelected,
                exactMountSelected);
        }

        public static bool IsExpectedActionSelection(
            bool turnBasedCombat,
            bool actionIsMountOwned,
            bool unifiedMountedTurn,
            bool exactRiderSelected,
            bool exactMountSelected)
        {
            if (unifiedMountedTurn)
            {
                return exactRiderSelected;
            }

            return actionIsMountOwned && turnBasedCombat
                ? exactMountSelected
                : exactRiderSelected;
        }
    }

    // F4 (WORLD-02). Native ClickMapObjectHandler.TryInteract stops the shared travel of every selected unit with
    // UnitCommands.InterruptMove once an accepted trap order chose its one interactor, before that unit's command runs.
    // A selected mounted rider owns no move of its own (the pair's travel is the mount's command), so the same native
    // stop is forwarded to that rider's mount, inside that accepted click only. Hover/prediction simulations, other
    // InterruptMove callers and unmounted or unrelated units are never touched.
    public static class MountedTrapGroupStopPolicy
    {
        // The prefix opens the scope only where native TryInteract runs its stop loop (a trap interaction or an object with a
        // visible trap) and never for a hover or prediction simulation.
        public static bool OpensTrapClickScope(bool disableTrapInteraction, bool hasVisibleTrap, bool simulatingClick)
        {
            return (disableTrapInteraction || hasVisibleTrap) && !simulatingClick;
        }

        public static bool ShouldForward(bool insideAcceptedTrapClick, bool simulatingClick, bool pairMounted, bool stoppedUnitIsExactRider)
        {
            return insideAcceptedTrapClick && !simulatingClick && pairMounted && stoppedUnitIsExactRider;
        }

        // The click's own scope: the native stop loop only runs for a trap interaction (or an object with a visible trap)
        // that native SelectUnit accepted, synchronously inside TryInteract on the frame it began.
        public static bool IsAcceptedTrapClickScope(bool scopeOpen, int scopeFrame, int currentFrame, bool trapInteraction)
        {
            return scopeOpen && scopeFrame == currentFrame && trapInteraction;
        }
    }

    // F4 (WORLD-01). Native GroupManager.Go (the Beneath the Stolen Lands entrance, the capital exit and every party
    // selection screen) calls Player.ReInitPartyCharacters: every party character is removed (HandleCompanionRemoved,
    // staying in game when it is in the new group) and then re-added (HandleCompanionActivated), even with the party
    // unchanged. Only a removal that stays in game inside that re-initialisation waits for its end; a character that
    // is back in the party then keeps its exact pair, and anything else is cleaned up exactly as a removal always was.
    public enum PartyRemovalDisposition
    {
        CleanupNow,
        DeferUntilReinitialisationEnds
    }

    // F4 (GAIT-01), presentation only. Native UnitAnimationController.TickOnUnit picks the Slow gait whenever a
    // Normal-type move's current speed is under 95% of the unit's blueprint speed. Out of combat a group click paces
    // every selected unit to the slowest selected speed (never under 30 ft), so the 50 ft Horse walks behind 30 ft
    // companions at a pace (>= 3.048 m/s) its own run cycle covers. Only that party-paced case shows the run cycle;
    // the agent speed, the command's speed limit and the native speed are untouched. Explicit Slow, Stealth and Charge,
    // combat and turn-based movement, an impaired Horse (encumbrance, terrain, conditions) and a pace below the floor
    // keep the native gait.
    public enum NativeGait
    {
        Slow,
        Normal,
        Charge,
        Stealth
    }

    public static class MountedHorseGaitPolicy
    {
        public const float NativeSlowThreshold = 0.95f;
        // The lowest pace shown as a trot, as a fraction of the Horse's own blueprint speed (native group floor 30 ft
        // against the 50 ft Horse is 0.6).
        public const float MinimumTrotFraction = 0.55f;

        public static bool ShouldShowTrot(bool exactHorse, NativeGait nativeGait, NativeGait commandMovementType, bool overrideSpeed,
            bool inCombat, bool turnBasedCombat, float? speedLimitMps, float ownModifiedSpeedMps, float blueprintSpeedMps)
        {
            if (!exactHorse || nativeGait != NativeGait.Slow || commandMovementType != NativeGait.Normal || overrideSpeed ||
                inCombat || turnBasedCombat || !speedLimitMps.HasValue || !(blueprintSpeedMps > 0f))
                return false;
            var limit = speedLimitMps.Value;
            // Pacing is the only reason for the Slow gait: the Horse itself is unimpaired and the limit is what binds.
            return ownModifiedSpeedMps >= NativeSlowThreshold * blueprintSpeedMps && limit < ownModifiedSpeedMps &&
                limit >= MinimumTrotFraction * blueprintSpeedMps;
        }
    }

    public static class PartyReinitialisationRetentionPolicy
    {
        public static PartyRemovalDisposition OnRemoved(bool insideReinitialisation, bool removedUnitStaysInGame)
        {
            return insideReinitialisation && removedUnitStaysInGame
                ? PartyRemovalDisposition.DeferUntilReinitialisationEnds
                : PartyRemovalDisposition.CleanupNow;
        }

        public static bool RetainAtEnd(bool backInParty, bool inGame)
        {
            return backInParty && inGame;
        }
    }

    public static class MountedInteractionRoutingPolicy
    {
        public static bool ShouldRouteExactDoor(
            bool relationshipMounted,
            bool commandOwnerIsExactRider,
            bool commandIsExactStockInteraction,
            bool targetIsExactStandardDoor)
        {
            return relationshipMounted && commandOwnerIsExactRider &&
                commandIsExactStockInteraction && targetIsExactStandardDoor;
        }

        public static bool CanAdmitInCurrentTurn(
            bool turnBasedCombat,
            bool currentTurnIsExactRider,
            bool turnPreparing,
            bool turnActing)
        {
            return !turnBasedCombat || currentTurnIsExactRider && (turnPreparing || turnActing);
        }
    }

    public static class MountedDistanceDoorFixturePolicy
    {
        public static bool CanTemporarilyEnable(
            bool doorCanInteract,
            bool doorEnabled,
            bool disableOnOpen,
            bool playerInCombat)
        {
            return !doorCanInteract && !doorEnabled && disableOnOpen && !playerInCombat;
        }

        public static bool IsExactlyRestored(
            bool leaseCaptured,
            bool originalOpen,
            bool originalEnabled,
            bool currentOpen,
            bool currentEnabled)
        {
            return leaseCaptured && originalOpen == currentOpen && originalEnabled == currentEnabled;
        }
    }

    public static class MountedDistanceDoorTraversalReadinessPolicy
    {
        public static bool IsReady(
            bool doorOpen,
            bool disableNavmeshCutWhenOpen,
            bool navmeshCutPresent,
            bool navmeshCutEnabled,
            bool navmeshCutRequiresUpdate)
        {
            if (!doorOpen)
            {
                return false;
            }

            if (!disableNavmeshCutWhenOpen)
            {
                return true;
            }

            return navmeshCutPresent && !navmeshCutEnabled && !navmeshCutRequiresUpdate;
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
