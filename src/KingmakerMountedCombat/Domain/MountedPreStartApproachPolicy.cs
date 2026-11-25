namespace KingmakerMountedCombat.Domain
{
    public enum MountedEarlyStriker
    {
        None,
        Rider,
        Mount
    }

    public enum MountedPreStartApproachAction
    {
        // Nothing to do (no pre-start approach applies, or nothing is live).
        None,
        // Submit the pair command's exact delegated mount move toward the target.
        Issue,
        // The live delegated move continues.
        Keep,
        // Release the live delegated move exactly (range reached, approach no longer lawful, or the order is leaving).
        Stop,
        // Release the live move and submit a new one toward the target's new position.
        Reissue,
        // Not now: foreign work holds the mount, or a failed route is waiting for its retry time.
        Wait
    }

    // MMR-05 (review R3-F1). Native UnitActionController.TickCommand (2.1.7b) ticks an unstarted command's approach while
    // ShouldStartCommand still refuses it (preparation, initiative wait via CanActInCombat, Standard cooldown, busy hands):
    // an unmounted attacker walks toward its target during its cooldown and swings once it is both close and ready. The pair
    // command is that unstarted command in the rider's Standard slot; its approach is the mount's movement, so while native
    // has not started it, the same native TickApproaching drives this decision for the pair command's own delegated move.
    // Native alone still decides the start, the attack and its expenditure; nothing here starts, readies or spends anything.
    public static class MountedPreStartApproachPolicy
    {
        // Bounded retries of a route that finished without reaching legal range before native starts the command; after
        // that the started command's own bounded approach decides (its ApproachFailure terminal).
        public const int MaximumFailedRoutes = 3;
        public const double RetrySeconds = 1d;

        public static MountedPreStartApproachAction Decide(
            bool commandInSlot,
            bool realTime,
            bool approachPermitted,
            bool commandStarted,
            bool commandFinished,
            bool targetPerceived,
            bool movementForbidden,
            bool strikeAdmissibleNow,
            bool moveLive,
            bool moveOwnsMountSlot,
            bool targetDisplaced,
            bool mountSlotsFree,
            int failedRoutes,
            bool retryDue)
        {
            // A queued command is never ticked by native (UnitActionController walks Commands.Raw only), exactly as an
            // unmounted attacker's queued UnitAttack does not move until queue promotion places it in a slot.
            var applies = commandInSlot && realTime && approachPermitted && !commandStarted && !commandFinished && targetPerceived && !movementForbidden;
            if (!applies) return moveLive ? MountedPreStartApproachAction.Stop : MountedPreStartApproachAction.None;
            // In legal range: hold position, as an unmounted attacker stops at its approach radius and waits to start.
            if (strikeAdmissibleNow) return moveLive ? MountedPreStartApproachAction.Stop : MountedPreStartApproachAction.None;
            if (moveLive)
            {
                // A live move that no longer owns the mount's Move slot is not ours to drive further.
                if (!moveOwnsMountSlot) return MountedPreStartApproachAction.Stop;
                return targetDisplaced ? MountedPreStartApproachAction.Reissue : MountedPreStartApproachAction.Keep;
            }
            if (!mountSlotsFree) return MountedPreStartApproachAction.Wait;
            if (failedRoutes >= MaximumFailedRoutes) return MountedPreStartApproachAction.None;
            return failedRoutes == 0 || retryDue ? MountedPreStartApproachAction.Issue : MountedPreStartApproachAction.Wait;
        }

        // Review R3-F1: which actor's strike an out-of-reach melee order is dispatched for while no actor is strike-ready.
        // An explicit horse order is the horse's; an ordinary order is the rider's when the rider may make it (melee, no
        // restriction, able to act and attack), otherwise the horse's when the horse may; else none.
        public static MountedEarlyStriker SelectStriker(bool explicitMountOrder, bool riderMayStrike, bool mountMayStrike) =>
            explicitMountOrder ? (mountMayStrike ? MountedEarlyStriker.Mount : MountedEarlyStriker.None) :
            riderMayStrike ? MountedEarlyStriker.Rider : MountedEarlyStriker.None;

        // Real-time dispatch of the selected striker's order while no actor is strike-ready: only when the planned strike is
        // not admissible where the pair stands and movement is lawful. The order then waits in its native slot exactly as an
        // unmounted UnitAttack does; native ShouldStartCommand alone decides the start (initiative, cooldown, hands).
        public static bool DispatchBeforeStrikeReadiness(
            bool realTime,
            bool strikerSelected,
            bool movementForbidden,
            bool plannedStrikeAdmissibleNow,
            bool riderStandardReady,
            bool mountStandardReady) =>
            realTime && strikerSelected && !movementForbidden && !plannedStrikeAdmissibleNow && !riderStandardReady && !mountStandardReady;
    }
}
