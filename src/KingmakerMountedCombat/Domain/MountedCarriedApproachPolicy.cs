using System;

namespace KingmakerMountedCombat.Domain
{
    // Sprint 3 (TRAP-01, SPELL-01). The decisions behind one "carried approach": the mount supplies the
    // movement for the rider's OWN unstarted native command (a spell, item or touch delivery through
    // UnitUseAbility, or a discovered-trap interaction through UnitInteractWithObject). The native command
    // keeps every cost, charge, check, roll and effect; the only thing replaced is the native per-tick
    // rider approach (UnitCommand.TickApproaching -> UnitEntityView.MoveTo), which paths the carried
    // rider's disabled stock agent and therefore never closes the distance. Kept free of engine types
    // so the regressions run offline.
    public enum MountedCarriedCommandKind
    {
        None,
        Ability,
        TrapInteraction
    }

    public enum MountedCarriedApproachDisposition
    {
        // The command is not one this pair carries: native approach runs untouched.
        NotCarried,
        // The rider's own unstarted ability or trap interaction: the mount approaches for it.
        Carry
    }

    // What the driver does when the mount's delegated move ended short of the command's range.
    public enum MountedCarriedMoveEndAction
    {
        // Real time: the native re-approach cooldown and the bounded progress rule (Sprint 2 lifecycle).
        LifecycleRetry,
        // Turn-based combat: the native "path ended, unit stopped" semantics end the command itself
        // (UnitActionController.TickCommandTurnBased force-finishes a should-approach command whose
        // NextApproachTime is set once the executor stops moving). The driver signals exactly that
        // and never re-attempts on its own; a held touch charge stays held natively.
        SignalNativeApproachEnd
    }

    public static class MountedCarriedApproachPolicy
    {
        // A live delegated move is replanned when the command's approach point drifted this far from the
        // point the move was planned for (a moving recipient). The replan is a normal approach end, so the
        // bounded no-progress rule still ends a chase that never closes.
        public const float ReplanEpsilonMetres = 0.75f;
        // Native UnitInteractWithObject safe-point rule (2.1.7b UpdateTarget): the interaction stops where the
        // next step of (corpulence + this margin) toward the trap would enter its trigger zone.
        public const float NativeTrapProbeMarginMetres = 0.25f;
        public const float NativeTrapStopOffsetMetres = 0.01f;

        public static MountedCarriedApproachDisposition Decide(
            bool pairMounted,
            bool executorIsRider,
            bool commandStarted,
            bool commandFinished,
            MountedCarriedCommandKind kind,
            bool relationshipControlShell,
            bool chargeShell,
            bool aiOriginated,
            bool cursorSimulation)
        {
            if (!pairMounted || !executorIsRider || commandStarted || commandFinished || cursorSimulation)
            {
                return MountedCarriedApproachDisposition.NotCarried;
            }
            if (kind == MountedCarriedCommandKind.None || relationshipControlShell || chargeShell || aiOriginated)
            {
                return MountedCarriedApproachDisposition.NotCarried;
            }
            return MountedCarriedApproachDisposition.Carry;
        }

        public static MountedCarriedMoveEndAction OnDelegatedMoveEnded(bool turnBasedCombat)
        {
            return turnBasedCombat
                ? MountedCarriedMoveEndAction.SignalNativeApproachEnd
                : MountedCarriedMoveEndAction.LifecycleRetry;
        }

        public static bool ShouldReplan(float plannedToCurrentApproachPointMetres, float epsilonMetres)
        {
            if (float.IsNaN(plannedToCurrentApproachPointMetres) || float.IsInfinity(plannedToCurrentApproachPointMetres) ||
                float.IsNaN(epsilonMetres) || float.IsInfinity(epsilonMetres) || epsilonMetres < 0f)
            {
                return false;
            }
            return plannedToCurrentApproachPointMetres > epsilonMetres;
        }

        // Whether the carried command the mount is approaching for must be released because the rider was
        // given a new command of its own (native UnitCommands.Run replaces the running command of the same
        // slot and interrupts every other unstarted command that is not already in range).
        public static bool ReleasesOnRiderRequest(bool carrying, bool requestIsForRider, bool requestIsTheCarriedCommand)
        {
            return carrying && requestIsForRider && !requestIsTheCarriedCommand;
        }

        public static string Feedback(MountedCarriedCommandKind kind, string mountDisplayName)
        {
            switch (kind)
            {
                case MountedCarriedCommandKind.TrapInteraction:
                    return "Mounted trap interaction accepted: " + mountDisplayName + " approach, rider attempt.";
                case MountedCarriedCommandKind.Ability:
                    return "Mounted ability approach accepted: " + mountDisplayName + " approach, rider cast or delivery.";
                default:
                    return "Mounted approach not carried.";
            }
        }

        public static string ExhaustedFeedback(MountedCarriedCommandKind kind, string mountDisplayName, string reason)
        {
            return (kind == MountedCarriedCommandKind.TrapInteraction ? "Mounted trap interaction" : "Mounted ability approach") +
                " cancelled: " + mountDisplayName + " cannot reach the target (" + (reason ?? "no progress") + ").";
        }
    }
}
