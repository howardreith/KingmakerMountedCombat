using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    public enum MountedAutomaticProposalDecision
    {
        Refuse,
        ContinueOwnIntent,
        Admit
    }

    // PR-1 round 11 (R10-F1..F4): the one current admission answer for an unreleased automatic order, in precedence order: actual target
    // invalidation, then perception loss, then native ineligibility of a still valid target (BlueprintAiAction.IsTargetValid's restricted
    // rule with the action's current range). Typed, so no observer derives precedence from a reason text.
    public enum MountedAutomaticOrderAdmission
    {
        Admitted,
        TargetInvalid,
        TargetUnperceived,
        OutsideNativeAdmission
    }

    // One occupied rider command slot as native UnitCommands.Raw holds it.
    public struct MountedAutomaticSlotFacts
    {
        public bool Finished;
        // Native UnitCommand.AiAction is set: the brain's own command, never a player order.
        public bool AiOrigin;
        // The intent's own pair command or a native reaction (attack of opportunity): neither is a newer order.
        public bool PairOwnedOrReaction;
        // A Free or Swift command already in reach. Native Run leaves the Standard slot running beside it (a swift
        // self-buff, a kineticist blade's activation), so it never displaces an attack routine.
        public bool CoexistsWithAttack;
    }

    // Everything the native party-brain proposal is admitted from, read by the controller from the live pair at the
    // UnitCommands.Run boundary.
    public struct MountedAutomaticProposalFacts
    {
        public bool AiOrigin;
        public bool ExactMountedPair;
        public bool TurnBased;
        public bool OwnerIsRider;
        public bool RiderDirectlyControllable;
        public bool RiderAiEnabled;
        public bool RiderBrainActive;
        public bool StopSuppressed;
        public bool MovementForbidden;
        public bool TargetInRiderReach;
        public bool TargetValid;
        public bool ArmedAction;
        public bool PlayerIntentLive;
        public bool AutomaticIntentLive;
        public bool AutomaticIntentOnThisTarget;
        public bool PairCommandActive;
        public bool MountOperationLive;
        public bool NewerPlayerOperation;
        // Known issues Sprint 6 (A04): the rider carries native UnitCondition.CanNotAttack, the condition native
        // UnitAttack.ShouldBeInterrupted ends every attack for. Read fresh at each proposal; nothing is stored.
        public bool RiderCannotAttack;
    }

    // AI-01. Installed 2.1.7b facts: AiBrainController.SelectAction proposes through UnitCommands.Run with AiAction set
    // and CreatedByPlayer false, about every 0.3 s while no command forbids it (AiCanInterrupt). A proposal's target comes
    // from native perception, hostility, reachability and scoring; nothing here scans or scores a target. The pair
    // controller stays the one dispatcher: an admitted proposal becomes the same intent a click makes, so full-attack
    // planning, cooldowns and the native strike sequence are unchanged, and a new target never buys a new routine.
    public static class MountedAutomaticAttackPolicy
    {
        public static MountedAutomaticProposalDecision Decide(MountedAutomaticProposalFacts facts, out string refusal)
        {
            refusal = FirstRefusal(facts);
            if (refusal != null) return MountedAutomaticProposalDecision.Refuse;
            return facts.AutomaticIntentLive
                ? MountedAutomaticProposalDecision.ContinueOwnIntent
                : MountedAutomaticProposalDecision.Admit;
        }

        private static string FirstRefusal(MountedAutomaticProposalFacts facts)
        {
            if (!facts.AiOrigin) return "not-native-ai-origin";
            if (!facts.ExactMountedPair) return "no-exact-mounted-pair";
            if (facts.TurnBased) return "turn-based";
            if (!facts.OwnerIsRider) return "not-rider-brain";
            // A live player intent is never displaced, whichever target the brain prefers.
            if (facts.PlayerIntentLive) return "player-intent-live";
            if (facts.AutomaticIntentLive)
                return facts.AutomaticIntentOnThisTarget ? null : "automatic-intent-holds-its-target";
            if (facts.ArmedAction) return "armed-action";
            if (facts.PairCommandActive) return "pair-command-active";
            if (facts.MountOperationLive) return "mount-operation-live";
            if (facts.NewerPlayerOperation) return "player-operation-live";
            if (!facts.RiderDirectlyControllable) return "rider-not-directly-controllable";
            if (!facts.RiderAiEnabled) return "ai-off";
            if (!facts.RiderBrainActive) return "brain-inactive";
            if (facts.StopSuppressed) return "stop-suppressed";
            // A04: native would interrupt the attack before it starts, and the brain proposes again about every 0.3 s, so
            // each admission would only create and retire a pair command. The next proposal after the condition clears is
            // admitted as usual. Player orders never pass here.
            if (facts.RiderCannotAttack) return "rider-cannot-attack";
            if (!facts.TargetValid) return "target-invalid";
            if (facts.MovementForbidden && !facts.TargetInRiderReach) return "hold-out-of-reach";
            return null;
        }

        // A live rider order the brain did not make, or anything queued on either actor. A queued future order counts
        // for automatic admission: the proposal's native Run would clear that queue.
        public static bool HasPlayerOperation(IEnumerable<MountedAutomaticSlotFacts> riderSlots, int queuedCommands)
        {
            if (queuedCommands > 0) return true;
            foreach (var slot in riderSlots)
                if (!slot.Finished && !slot.AiOrigin && !slot.PairOwnedOrReaction && !slot.CoexistsWithAttack) return true;
            return false;
        }

        // Why a live intent stops repeating. A newer player operation retires an intent of either origin (a repeating
        // dispatch would otherwise replace a cast waiting to start); the rest are the automatic intent's own boundaries,
        // re-read every update because they can change between two proposals.
        // The retirement for failed stationary geometry alone (restricted travel, the strike not admissible from where the pair stands).
        public const string HoldOutOfReach = "hold out of reach";

        public static string RetireReason(
            bool realTime,
            bool automatic,
            bool riderDirectlyControllable,
            bool riderAiEnabled,
            bool riderBrainActive,
            bool stopSuppressed,
            bool movementForbidden,
            bool targetInRiderReach,
            bool playerOperation)
        {
            if (!realTime) return automatic ? "automatic intent left real time" : null;
            if (playerOperation) return "newer player operation";
            if (!automatic) return null;
            if (!riderDirectlyControllable) return "rider no longer directly controllable";
            if (!riderAiEnabled) return "ai off";
            if (!riderBrainActive) return "brain inactive";
            if (stopSuppressed) return "stop";
            if (movementForbidden && !targetInRiderReach) return HoldOutOfReach;
            return null;
        }
    }

    // The per-pair boundary of the automatic path. Native Stop (SelectionManagerBase.Stop) only clears Hold, unstarted
    // commands and both targets; it arms nothing, so a live brain would propose again within 0.4 s. The gate holds the
    // pair's proposals back until a deliberate new player order, an explicit AI off/on, or the end of the combat, mode
    // or relationship generation. Never persisted: a load starts clear.
    //
    // The player-move hold mirrors native UnitMoveTo.OnAction, which holds the unit that moved after a player order in
    // combat. The mount executes the pair's move, so the rider's own HoldState never records it. The mount's HoldState
    // alone cannot stand in for it, because the pair command's delegated approach is a UnitMoveTo as well:
    // MountedPlayerMoveWatch reads it only for the exact explicit move.
    public sealed class MountedAutomaticAttackGate
    {
        private long stopGeneration;
        private bool stopArmed;
        private bool aiOffSinceStop;

        public bool PlayerMoveHold { get; private set; }

        public bool IsSuppressed(long generation)
        {
            return stopArmed && stopGeneration == generation;
        }

        public void ObserveStop(long generation)
        {
            stopArmed = true;
            stopGeneration = generation;
            aiOffSinceStop = false;
            PlayerMoveHold = false;
        }

        // Only the exact move's own native completion arrives here; issuing an order is ObservePlayerOrder.
        public void ObservePlayerMoveCompleted()
        {
            PlayerMoveHold = true;
        }

        public void ObservePlayerOrder()
        {
            stopArmed = false;
            PlayerMoveHold = false;
        }

        public void Tick(long generation, bool exactMountedPair, bool inCombat, bool turnBased, bool aiEnabled)
        {
            if (!exactMountedPair || !inCombat || turnBased || stopArmed && stopGeneration != generation)
            {
                Clear();
                return;
            }
            if (!stopArmed) return;
            if (!aiEnabled) aiOffSinceStop = true;
            else if (aiOffSinceStop) stopArmed = false;
        }

        public void Clear()
        {
            stopArmed = false;
            aiOffSinceStop = false;
            PlayerMoveHold = false;
        }
    }

    public enum MountedQueuedAutomaticRun
    {
        NotAutomatic,
        Execute,
        RefuseFinished,
        RefuseStale
    }

    // Which exact command is the pair's current automatic one. Installed 2.1.7b: UnitCommands.Run(cmd, fromQueue,
    // doNotClearQueue) clears the owner's Hold for any command without AiAction, at immediate admission and again when
    // RemoveFinishedAndUpdateQueue later runs it from the queue, and the public Run's own hooks see only the first.
    // A custom handler (a kineticist blade's activation) queues the command first and runs its prerequisite with
    // doNotClearQueue, inheriting the same AiAction. Only that one native write is withheld, so Hold always stays the
    // current value: a Hold pressed or cleared by a player between the two boundaries needs nothing restored.
    public sealed class MountedAutomaticRunLedger
    {
        private object command;
        private long generation;

        public void Bind(object automaticCommand, long pairGeneration)
        {
            command = automaticCommand;
            generation = pairGeneration;
        }

        public void Clear()
        {
            command = null;
        }

        public bool Owns(object candidate, long pairGeneration)
        {
            return candidate != null && ReferenceEquals(command, candidate) && generation == pairGeneration;
        }

        public bool NativeRunKeepsHold(object running, object firstQueued, bool preparesQueuedCommand, long pairGeneration)
        {
            return Owns(running, pairGeneration) || preparesQueuedCommand && Owns(firstQueued, pairGeneration);
        }

        // The later Run reaches none of the public Run's admission, so a command that outlived its intent stops here.
        public MountedQueuedAutomaticRun DecideQueuedRun(object queued, bool automaticOrigin, bool finished, bool exactMountedPair, long pairGeneration)
        {
            if (!automaticOrigin) return MountedQueuedAutomaticRun.NotAutomatic;
            if (finished) return MountedQueuedAutomaticRun.RefuseFinished;
            return exactMountedPair && Owns(queued, pairGeneration)
                ? MountedQueuedAutomaticRun.Execute
                : MountedQueuedAutomaticRun.RefuseStale;
        }

        // Native Run clears the queue without interrupting what it holds: an unstarted command in neither place never runs.
        public static bool IsOrphaned(bool automaticOrigin, bool started, bool finished, bool inSlot, bool inQueue)
        {
            return automaticOrigin && !started && !finished && !inSlot && !inQueue;
        }
    }

    public enum MountedPlayerMoveOutcome
    {
        None,
        Pending,
        CompletedHeld,
        CompletedNotHeld,
        Interrupted,
        Lost
    }

    // Separates "this particular player ground move actually completed" from "a new order was issued". Installed 2.1.7b:
    // UnitMoveTo.OnAction runs only on arrival, and sets the mover's Hold only when that arrival is in real-time combat,
    // so the exact move's native result answers both questions at the native boundary. Its Result is Interrupt, without
    // OnAction, when it is replaced, stopped or blocked. A move the queue dropped has no result at all.
    public sealed class MountedPlayerMoveWatch
    {
        private long generation;

        public object Move { get; private set; }

        // The exact unit the ground click itself ran: approaches for attacks, spells, traps, doors or recovery are
        // UnitMoveTo as well, and may carry CreatedByPlayer for native routing, but never pass through the ground click.
        // playerOrder: CreatedByPlayer, or (known issues M04 closeout) the exact native cannot-attack click approach the
        // ground order itself is running, which native never marks CreatedByPlayer.
        public static bool IsExplicitGroundMove(bool groundOrderOpen, bool exactMoveType, bool playerOrder, bool aiOrigin,
            bool ranOnPairMover, bool admitted)
        {
            return groundOrderOpen && exactMoveType && playerOrder && !aiOrigin && ranOnPairMover && admitted;
        }

        public void Bind(object move, long pairGeneration)
        {
            Move = move;
            generation = pairGeneration;
        }

        public void Clear()
        {
            Move = null;
        }

        // Known issues sprint 3: the watched order answers only for the pair generation it was bound for, so a pair
        // that mounted again cannot inherit an order from before its next update settles the watch.
        public bool IsCurrent(object move, long pairGeneration)
        {
            return move != null && ReferenceEquals(move, Move) && generation == pairGeneration;
        }

        public MountedPlayerMoveOutcome Observe(long pairGeneration, bool exactMountedPair, bool finished, bool completedByAction,
            bool moverHeld, bool inSlotOrQueue)
        {
            if (Move == null) return MountedPlayerMoveOutcome.None;
            var outcome = MountedPlayerMoveOutcome.Pending;
            if (!exactMountedPair || generation != pairGeneration) outcome = MountedPlayerMoveOutcome.Lost;
            else if (finished)
                outcome = !completedByAction ? MountedPlayerMoveOutcome.Interrupted :
                    moverHeld ? MountedPlayerMoveOutcome.CompletedHeld : MountedPlayerMoveOutcome.CompletedNotHeld;
            else if (!inSlotOrQueue) outcome = MountedPlayerMoveOutcome.Lost;
            if (outcome != MountedPlayerMoveOutcome.Pending) Move = null;
            return outcome;
        }
    }
}
