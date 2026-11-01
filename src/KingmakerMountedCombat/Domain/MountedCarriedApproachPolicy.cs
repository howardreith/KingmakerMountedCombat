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

        // PR #8 review correction A. What a request that native UnitCommands.Run ACTUALLY executed does to the
        // carried approach. Decided after Run from what Run did, never before it from what it might do: native Run
        // replaces the same-slot command and interrupts other unstarted commands that are not in range, queues a
        // command behind a busy slot, and leaves unrelated slots alone. A request any KMC gate refused never reaches
        // this decision (MountedCarriedRequestFlow), so it cannot release anything.
        public static MountedCarriedRequestAction AfterAdmittedRequest(
            bool riderRequest,
            bool mountRequest,
            bool requestIsCarriedCommand,
            bool requestIsDelegatedMove,
            bool playerRequest,
            bool carriedCommandDisplaced,
            bool mountMovementDisplaced)
        {
            // The carried command's own (re)run and the carrier's own delegated move never release the carrier.
            if (requestIsCarriedCommand || requestIsDelegatedMove) return MountedCarriedRequestAction.Keep;
            if (riderRequest)
            {
                // Native Run displaced the carried command (replaced it, or interrupted it as an out-of-range
                // unstarted command): its approach ends with it. A request native Run queued or ran beside it
                // (another slot that does not displace it) leaves the carried command and its move alone.
                return carriedCommandDisplaced ? MountedCarriedRequestAction.Release : MountedCarriedRequestAction.Keep;
            }
            if (mountRequest && mountMovementDisplaced)
            {
                // The mount's movement now belongs to another command, so the carrier owns nothing to drive. Only an
                // ordinary player order redirects the pair and ends the rider's pending command; an AI or other
                // non-player request releases the movement but has no authority over the rider's command, which waits
                // natively until the mount is free.
                return playerRequest && !carriedCommandDisplaced
                    ? MountedCarriedRequestAction.ReleaseAndInterrupt
                    : MountedCarriedRequestAction.Release;
            }
            return MountedCarriedRequestAction.Keep;
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

    public enum MountedCarriedRequestAction
    {
        Keep,
        Release,
        ReleaseAndInterrupt
    }

    // The engine-facing facts the request flow reads. Identities are opaque so the flow runs offline.
    public interface IMountedCarriedRequestHost
    {
        // The bound carried approach (live or just ended), or null.
        object LiveApproach { get; }
        bool IsRiderContainer(object container);
        bool IsMountContainer(object container);
        bool IsCarriedCommand(object approach, object command);
        bool IsDelegatedMove(object approach, object command);
        // An ordinary player order (CreatedByPlayer, not AI-originated).
        bool IsPlayerRequest(object command);
        // Read after native Run: the carried command finished or left every rider slot and the queue.
        bool CarriedCommandDisplaced(object approach);
        // Read after native Run: another live command now owns the mount's movement.
        bool MountMovementDisplaced(object approach);
        void Release(object approach, string reason);
        void InterruptCarriedCommand(object approach);
    }

    // One admitted request's claim on the carried approach it saw at admission.
    public sealed class MountedCarriedRequestTicket
    {
        internal MountedCarriedRequestTicket(object approach, object container, object command)
        {
            Approach = approach;
            Container = container;
            Command = command;
        }

        internal object Approach { get; }
        internal object Container { get; }
        internal object Command { get; }
        internal bool Consumed { get; set; }
    }

    // PR #8 review correction A: the UnitCommands.Run prefix evaluates every KMC gate first, without touching the
    // carried approach. Only a request every gate admitted gets a ticket; the Run postfix completes the ticket from what
    // native Run actually did. A refused request (a wrong-turn door click, a refused stock attack, a rejected stock
    // command) therefore leaves the carried command, its delegated move, a held charge and the approach progress
    // exactly as they were.
    public static class MountedCarriedRequestFlow
    {
        public static bool Admit(IMountedCarriedRequestHost host, object container, object command, Func<bool>[] gates,
            out MountedCarriedRequestTicket ticket)
        {
            ticket = null;
            if (gates != null)
            {
                for (var index = 0; index < gates.Length; index++)
                {
                    if (gates[index] != null && !gates[index]()) return false;
                }
            }
            var approach = host?.LiveApproach;
            if (approach != null && (host.IsRiderContainer(container) || host.IsMountContainer(container)))
            {
                ticket = new MountedCarriedRequestTicket(approach, container, command);
            }
            return true;
        }

        // finalCommand: the command native Run executed (a gate may route the request to another command).
        public static MountedCarriedRequestAction Complete(IMountedCarriedRequestHost host, MountedCarriedRequestTicket ticket, object finalCommand)
        {
            if (host == null || ticket == null || ticket.Consumed) return MountedCarriedRequestAction.Keep;
            ticket.Consumed = true;
            // A native boundary inside Run (the carried command's own end, a re-bind to the new command) already
            // settled the approach this ticket saw: there is nothing left for this request to release.
            if (!ReferenceEquals(host.LiveApproach, ticket.Approach)) return MountedCarriedRequestAction.Keep;
            var command = finalCommand ?? ticket.Command;
            var riderRequest = host.IsRiderContainer(ticket.Container);
            var action = MountedCarriedApproachPolicy.AfterAdmittedRequest(
                riderRequest,
                host.IsMountContainer(ticket.Container),
                host.IsCarriedCommand(ticket.Approach, command),
                host.IsDelegatedMove(ticket.Approach, command),
                host.IsPlayerRequest(command),
                host.CarriedCommandDisplaced(ticket.Approach),
                host.MountMovementDisplaced(ticket.Approach));
            if (action == MountedCarriedRequestAction.Keep) return action;
            host.Release(ticket.Approach, riderRequest ? "rider command admitted" : "mount command admitted");
            if (action == MountedCarriedRequestAction.ReleaseAndInterrupt) host.InterruptCarriedCommand(ticket.Approach);
            return action;
        }
    }

    // The engine-facing view of the mount's command slots for releasing the carrier's own delegated move.
    public interface IMountedOwnMoveHost
    {
        object MoveSlot { get; }
        // The command sits in its slot (native UnitCommands.Contains).
        bool Holds(object command);
        // Any unfinished command other than own in a slot, or any queued command.
        bool HasOtherLiveCommand(object own);
        bool IsFinished(object command);
        // Native Interrupt without the end event.
        void Interrupt(object command);
        // Native RemoveFinishedAndUpdateQueue: may Run the queue head (a successor).
        void SweepFinished();
        // Native UnitEntityView.StopMoving: stops whatever path the mount's agent follows.
        void StopAgent();
    }

    public struct MountedOwnMoveRelease
    {
        public bool Interrupted;
        public bool Swept;
        public bool AgentStopped;
        // Another command owns the mount's movement after the release; it was left untouched.
        public bool SuccessorRetained;
        // The carrier's own move is finished and no slot holds it any more.
        public bool OwnReleased;
    }

    // PR #8 review correction B: release only movement the carrier still owns. The agent is stopped only when no other
    // command owns the mount's movement after the interrupt and the sweep (the sweep can activate a queued successor),
    // and restoration means "our move is released", not "the mount has no commands".
    public static class MountedOwnMovePolicy
    {
        public static MountedOwnMoveRelease Release(IMountedOwnMoveHost host, object own)
        {
            var result = new MountedOwnMoveRelease();
            if (host == null || own == null)
            {
                result.OwnReleased = true;
                return result;
            }
            if (!host.IsFinished(own))
            {
                // Ending our own command never touches the agent (native OnEnded does not stop movement).
                host.Interrupt(own);
                result.Interrupted = true;
            }
            if (host.Holds(own) && host.IsFinished(own))
            {
                host.SweepFinished();
                result.Swept = true;
            }
            // Re-read after the interrupt and the sweep: either may have handed the movement to a successor.
            var moveSlot = host.MoveSlot;
            result.SuccessorRetained = host.HasOtherLiveCommand(own) || (moveSlot != null && !ReferenceEquals(moveSlot, own));
            if (!result.SuccessorRetained)
            {
                host.StopAgent();
                result.AgentStopped = true;
            }
            result.OwnReleased = host.IsFinished(own) && !host.Holds(own);
            return result;
        }
    }
}
